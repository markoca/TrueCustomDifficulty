using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootWorldPlacementContract;

// Actual production writer with randomized outcomes validated by invariants,
// never by expecting a fixed seed or a particular shuffled permutation.
internal static class Program
{
    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static readonly List<string[]> WeaponPool = Csv("world-weapon-shuffle-pool.csv", 3);
    private static readonly List<string[]> WeaponDirect = Csv("world-weapon-direct-manifest.csv", 7);
    private static readonly List<string[]> WeaponGenerator = Csv("world-weapon-shuffle-manifest.csv", 15);
    private static readonly List<string[]> AttachmentPool = Csv("world-attachment-shuffle-pool.csv", 3);
    private static readonly List<string[]> AttachmentDirect = Csv("world-attachment-direct-manifest.csv", 7);
    private static readonly List<string[]> AttachmentGenerator = Csv("world-attachment-generator-manifest.csv", 10);
    private static readonly List<string[]> Blueprint = Csv("blueprint-shuffle-manifest.csv", 8);
    private static readonly List<string[]> DetectorDirect = Csv("world-detector-direct-manifest.csv", 4);
    private static readonly List<string[]> DetectorStash = Csv("world-detector-stash-manifest.csv", 6);
    private static int Passed;
    private static string? Keep;

    private sealed class Parsed
    {
        public Dictionary<string,string> Fields {get;} = new(StringComparer.Ordinal);
        public HashSet<string> Removed {get;} = new(StringComparer.Ordinal);
        public Dictionary<string,string> References {get;} = new(StringComparer.Ordinal);
        public HashSet<string> Nodes {get;} = new(StringComparer.Ordinal);
        public string? Field(string path) => Fields.GetValueOrDefault(path);
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if(args.Length>0)
            {
                if(args.Length!=2 || args[0]!="--out")throw new ArgumentException("Use [--out PATH]");
                Keep=Path.GetFullPath(args[1]);Directory.CreateDirectory(Keep);
            }
            Inventory();
            foreach(var diff in new[]{"Novice","Stalker","Veteran","Master"})
                Case(diff,false,false,null,false,false,false,"vanilla");
            Case("Veteran",false,false,0.5,false,false,false,"inactive-density");
            foreach(var dlc in new[]{false,true})
            {
                Case("Veteran",dlc,false,null,false,true,false,"detectors");
                Case("Veteran",dlc,false,null,false,false,true,"blueprints");
                Case("Veteran",dlc,false,null,true,false,false,"attachments");
                foreach(var density in new[]{0.0,0.5,1.0})
                    Case("Veteran",dlc,true,density,false,false,false,"weapons");
            }
            Case("Veteran",true,false,null,false,true,true,"detectors-and-blueprints");
            Case("Veteran",true,false,null,true,true,false,"detectors-and-attachments");
            Check(Passed==20,"Expected 20 world-weapon, attachment, blueprint and detector production writer scenarios");
            Console.WriteLine($"PASS: {Passed} World Loot placement production BPatch scenarios");
            Console.WriteLine("NOT VERIFIED: original physical manifest ZIP provenance, Unreal loading, gameplay");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine("FAIL: "+e);return 1;}
    }

    private static void Inventory()
    {
        Check(WeaponPool.Count==36 && WeaponDirect.Count==362 && WeaponGenerator.Count==14,
            "Weapon pool and eligible direct/generator inventory drift");
        Check(AttachmentPool.Count==39 && AttachmentDirect.Count==7 && AttachmentGenerator.Count==60,
            "Attachment inventory drift");
        Check(Blueprint.Count==77 && DetectorDirect.Count==7 && DetectorStash.Count==1,
            "World blueprint or detector inventory drift");
        Check(WeaponDirect.Count(x=>x[0]=="baseGame")==282 &&
            WeaponDirect.Count(x=>x[0]=="dlc1")==80,"Weapon source DLC split");
        Check(AttachmentDirect.Count(x=>x[0]=="base")==5 &&
            AttachmentGenerator.Count(x=>x[0]=="base")==57,
            "Attachment source DLC split");
        Check(DetectorDirect.Count(x=>x[0]=="base")==4 &&
            DetectorDirect.Count(x=>x[0]=="dlc1")==3,
            "Detector source DLC split");
        var ids=new[]{"loot.worldWeaponShuffle","loot.worldWeaponDensity",
            "loot.worldAttachmentShuffle","loot.worldDetectors","loot.blueprintShuffle"};
        var vm=new MainViewModel(Catalog);
        Check(ids.All(id=>vm.AllParameters.Any(p=>p.Id==id)),"World placement control missing from actual ViewModel");
        Check(ids.Where(id=>id!="loot.worldWeaponDensity").All(id=>
            vm.AllParameters.Single(p=>p.Id==id).IsToggle),
            "Original four world toggles must remain booleans");
        Check(vm.AllParameters.Single(p=>p.Id=="loot.worldWeaponDensity").IsSlider,
            "World weapon density must stay dependent slider");
        Check(WeaponPool.Select(p=>p[0]).Distinct(StringComparer.Ordinal).Count()==36 &&
            AttachmentPool.Select(p=>p[0]).Distinct(StringComparer.Ordinal).Count()==39,
            "Regular weapon/attachment model pool contains duplicates");
    }

    private static void Case(string difficulty,bool dlc,bool weapons,double? density,
                             bool attachments,bool detectors,bool blueprints,string label)
    {
        var root=Path.Combine(Keep??Path.GetTempPath(),"TCD_World_Placement",Passed+"-"+label+(dlc?"-dlc":"-base"));
        try
        {
            var vm=new MainViewModel(Catalog){SelectedReferenceDifficulty=difficulty};
            var active=new HashSet<string>(StringComparer.Ordinal);
            void Toggle(string id,bool value)
            {
                if(!value)return;
                var p=vm.AllParameters.Single(x=>x.Id==id);
                Check(p.IsToggle && p.IsControlEnabled && !p.IsValueModifiedFromReference(),
                    label+": source toggle unavailable "+id);
                p.CurrentBoolean=true;
                Check(p.IsValueModifiedFromReference(),label+": unable to toggle "+id);
                active.Add(id);
            }
            Toggle("loot.worldWeaponShuffle",weapons);
            Toggle("loot.worldAttachmentShuffle",attachments);
            Toggle("loot.worldDetectors",detectors);
            Toggle("loot.blueprintShuffle",blueprints);
            if(density.HasValue)
            {
                var id="loot.worldWeaponDensity";
                var p=vm.AllParameters.Single(x=>x.Id==id);
                if(weapons)Check(p.IsControlEnabled,"Density should be enabled only after shuffle toggle");
                p.SnapCurrentNumberToMarker(density.Value);
                if(Math.Abs(density.Value-p.ReferenceNumber)>0.00000001
                    && p.IsControlEnabled) active.Add(id);
            }
            var observed=vm.AllParameters.Where(p=>p.Id.StartsWith("loot.",StringComparison.Ordinal)
                && p.IsValueModifiedFromReference() && p.IsControlEnabled).Select(p=>p.Id)
                .ToHashSet(StringComparer.Ordinal);
            Check(observed.SetEquals(active),label+": collateral active source edits");
            var result=new CfgPatchWriterService().Write(vm,Catalog,
                dlc?new GameInstallInfo{HasDlc1=true}:null,root);
            var documents=ReadOutput(root);
            Check(result.FileCount==documents.Count && result.ParameterCount==active.Count,
                label+": wrong production output file/parameter counts");
            var allowedFields=new HashSet<string>(StringComparer.Ordinal);
            var allowedRemovals=new HashSet<string>(StringComparer.Ordinal);
            var allowedRefs=new HashSet<string>(StringComparer.Ordinal);
            if(weapons)CheckWeapons(dlc,density??1.0,documents,allowedFields,allowedRemovals);
            if(attachments)CheckAttachments(dlc,documents,allowedFields,allowedRefs);
            if(detectors)CheckDetectors(dlc,documents,allowedRemovals);
            if(blueprints)CheckBlueprints(documents,allowedFields);
            var actualFields=documents.SelectMany(x=>x.Value.Fields.Keys.Select(k=>x.Key+"|"+k))
                .ToHashSet(StringComparer.Ordinal);
            var actualRemoved=documents.SelectMany(x=>x.Value.Removed.Select(k=>x.Key+"|"+k))
                .ToHashSet(StringComparer.Ordinal);
            var actualRefs=documents.SelectMany(x=>x.Value.References.Keys.Select(k=>x.Key+"|"+k))
                .ToHashSet(StringComparer.Ordinal);
            Check(actualFields.SetEquals(allowedFields),
                label+": missing or collateral physical world assignment, expected "+
                allowedFields.Count+" actual "+actualFields.Count+" extra=["+
                string.Join(";",actualFields.Except(allowedFields).Take(5))+"] missing=["+
                string.Join(";",allowedFields.Except(actualFields).Take(5))+"]");
            Check(actualRemoved.SetEquals(allowedRemovals),label+
                ": unexpected/missing world removal nodes, extra=["+
                string.Join(";",actualRemoved.Except(allowedRemovals).Take(5))+"]");
            Check(actualRefs.SetEquals(allowedRefs),label+": unexpected clone roots");
            Check(!documents.Keys.Any(k=>k.StartsWith("DLCGameData/",StringComparison.Ordinal))||dlc,
                label+": emitted DLC1 files without DLC installed");
            Console.WriteLine("PASS "+difficulty+" "+label+(dlc?" +DLC1":" BASE")
                +" ("+documents.Count+" CFG files, "+actualFields.Count+" fields, "+
                actualRemoved.Count+" removals)");
            Passed++;
        }
        finally{if(Keep is null && Directory.Exists(root))Directory.Delete(root,true);}
    }

    private static void CheckDetectors(bool dlc,Dictionary<string,Parsed> docs,
        HashSet<string> removals)
    {
        var direct=DetectorDirect.Where(r=>dlc||r[0]=="base").ToList();
        Check(direct.Count==(dlc?7:4),"Detector eligible actor count");
        foreach(var row in direct)
        {
            var key=row[1]+"|"+row[2];
            removals.Add(key);
            Check(docs.TryGetValue(row[1],out var doc)&&doc.Removed.Contains(row[2]),
                "Missing detector direct pickup actor removal "+key);
        }
        if(dlc)
        {
            var row=DetectorStash.Single();
            var node=row[2]+"/"+row[3];
            removals.Add(row[1]+"|"+node);
            Check(docs.TryGetValue(row[1],out var doc)&&doc.Removed.Contains(node),
                "Missing DLC1 X16 detector stash generator node removal");
        }
    }

    private static void CheckBlueprints(Dictionary<string,Parsed> docs,HashSet<string> fields)
    {
        Check(Blueprint.Count==77,"Blueprint world-only source list count");
        const string source="GameData/ItemGeneratorPrototypes.cfg";
        Check(docs.TryGetValue(source,out var doc),"No blueprint source CFG");
        var original=Blueprint.Select(r=>r[2]).ToList();
        var newValues=new List<string>();
        foreach(var r in Blueprint)
        {
            var path=r[1]+"/"+r[3];
            var sid=doc!.Field(path+"/ItemPrototypeSID");
            Check(sid is not null && sid!=r[2],
                "Blueprint derangement omitted original slot or left blueprint in place "+path);
            Check(doc.Field(path+"/Chance")=="1","Blueprint probability was not locked to 1 "+path);
            fields.Add(source+"|"+path+"/ItemPrototypeSID");
            fields.Add(source+"|"+path+"/Chance");
            newValues.Add(sid);
        }
        Check(original.OrderBy(x=>x,StringComparer.Ordinal)
            .SequenceEqual(newValues.OrderBy(x=>x,StringComparer.Ordinal)),
            "Blueprint permutation lost/duplicated authored blueprint SIDs");
    }

    private static void CheckAttachments(bool dlc,Dictionary<string,Parsed> docs,
        HashSet<string> fields,HashSet<string> refs)
    {
        var direct=AttachmentDirect.Where(r=>dlc||r[0]=="base").ToList();
        var generators=AttachmentGenerator.Where(r=>dlc||r[0]=="base").ToList();
        Check(direct.Count==(dlc?7:5) && generators.Count==(dlc?60:57),
            "Attachment eligible slot count drift");
        var pool=AttachmentPool.ToDictionary(x=>x[0],x=>x,StringComparer.Ordinal);
        var sources=new List<string>();
        var results=new List<string>();
        foreach(var row in direct)
        {
            var source=row[1],root=row[3],orig=row[4];
            var a=docs.GetValueOrDefault(source)?.Field(root+"/SpawnedPrototypeSID");
            var b=docs.GetValueOrDefault(source)?.Field(root+"/ItemSID");
            Check(a==b,"Direct attachment identity fields must be replaced together "+root);
            var sid=a??orig;
            CheckCompatible(pool,sid,row[5],row[6],"direct attachment "+root);
            sources.Add(orig);results.Add(sid);
            if(a!=null)
            {
                Check(a!=orig,"Direct attachment redundant vanilla edit");
                fields.Add(source+"|"+root+"/SpawnedPrototypeSID");
                fields.Add(source+"|"+root+"/ItemSID");
            }
        }
        var groups=generators.GroupBy(g=>g[5],StringComparer.Ordinal);
        foreach(var group in groups)
        {
            var first=group.First();
            var file="GameData/ItemGeneratorPrototypes.cfg";
            var clone=first[5];
            var expectedRef="refurl=../ItemGeneratorPrototypes.cfg;refkey="+first[4];
            var doc=docs.GetValueOrDefault(file);
            Check(doc is not null && doc.References.TryGetValue(clone,out var refInfo)
                && refInfo==expectedRef,"Attachment clone refurl/refkey or SID missing "+clone);
            refs.Add(file+"|"+clone);
            Check(doc.Field(clone+"/SID")==clone,"Attachment cloned generator missing SID "+clone);
            fields.Add(file+"|"+clone+"/SID");
            foreach(var row in group)
            {
                Check(row[2]==first[2] && row[1]==first[1] && row[4]==first[4],
                    "Attachment clone group mixes independent world physical actors");
                var path=clone+"/"+row[6]+"/ItemPrototypeSID";
                var sid=doc.Field(path);
                Check(sid is not null,"Missing cloned world attachment item "+path);
                CheckCompatible(pool,sid!,row[8],row[9],"attachment generator "+clone);
                sources.Add(row[7]);results.Add(sid!);
                fields.Add(file+"|"+path);
            }
            foreach(var redirect in first[3].Split(';',StringSplitOptions.RemoveEmptyEntries))
            {
                var path=first[2]+"/"+redirect+"/PrototypeSID";
                Check(docs.GetValueOrDefault(first[1])?.Field(path)==clone,
                    "Attachment generator clone is not wired to physical world actor "+path);
                fields.Add(first[1]+"|"+path);
            }
        }
        Check(sources.OrderBy(x=>x,StringComparer.Ordinal)
            .SequenceEqual(results.OrderBy(x=>x,StringComparer.Ordinal)),
            "World attachment shuffle does not preserve exact eligible source item multiset");
        Check(results.Count==(dlc?67:62),"Wrong complete attachment surface cardinality");
    }

    private static void CheckWeapons(bool dlc,double density,Dictionary<string,Parsed> docs,
        HashSet<string> fields,HashSet<string> removals)
    {
        var direct=WeaponDirect.Where(r=>dlc||r[0]=="baseGame").ToList();
        var generator=WeaponGenerator;
        Check(direct.Count==(dlc?362:282)&&generator.Count==14,
            "World weapon eligible surface cardinality");
        var pool=WeaponPool.ToDictionary(x=>x[0],x=>x,StringComparer.Ordinal);
        var total=0;
        var counts=new Dictionary<string,int>(StringComparer.Ordinal);
        void Accept(string sid,string family,string tier,string where)
        {
            CheckCompatibleWeapon(pool,sid,family,tier,where);
            counts[sid]=counts.GetValueOrDefault(sid)+1;
            total++;
        }
        foreach(var row in direct)
        {
            var file=ActorFile(row[1],row[2],row[3]);
            var path=row[3];
            var doc=docs.GetValueOrDefault(file);
            if(doc is not null)
            {
                Check(doc.Removed.Count==0 || doc.Removed.SetEquals(new[]{path}),
                    "Direct weapon actor has unrelated removal node "+file);
                if(doc.Removed.Contains(path))
                {
                    removals.Add(file+"|"+path);
                    continue;
                }
            }
            var assigned=doc?.Field(path+"/SpawnedPrototypeSID");
            var item=doc?.Field(path+"/ItemSID");
            Check(assigned==item,"Direct weapon replacement must patch both identifiers "+file);
            if(assigned!=null)
            {
                Check(assigned!=row[4],"Redundant direct world weapon replacement "+file);
                fields.Add(file+"|"+path+"/SpawnedPrototypeSID");
                fields.Add(file+"|"+path+"/ItemSID");
            }
            Accept(assigned??row[4],row[5],row[6],file);
        }
        const string generatorFile="GameData/ItemGeneratorPrototypes.cfg";
        var gen=docs.GetValueOrDefault(generatorFile);
        foreach(var row in generator)
        {
            var path=row[1]+"/"+row[6],file=generatorFile;
            if(gen?.Removed.Contains(path)==true)
            {
                removals.Add(file+"|"+path);
                continue;
            }
            var assigned=gen?.Field(path+"/ItemPrototypeSID");
            if(assigned!=null)
            {
                Check(assigned!=row[3],"Redundant generator item replacement "+path);
                fields.Add(file+"|"+path+"/ItemPrototypeSID");
                // If the original row has other mechanics, production must
                // preserve the authored source fields under replacement node.
                var names=new[]{"Chance","Weight","MinDurability","MaxDurability",
                    "AmmoMinCount","AmmoMaxCount","MinCount","MaxCount"};
                for(var i=0;i<names.Length;i++)
                {
                    var raw=row[7+i];
                    if(raw.Length==0)continue;
                    Check(gen!.Field(path+"/"+names[i])==raw,
                        "World weapon shuffle lost authored generator mechanic "+
                        path+"/"+names[i]);
                    fields.Add(file+"|"+path+"/"+names[i]);
                }
            }
            Accept(assigned??row[3],row[4],row[5],path);
        }
        var candidates=(dlc?376:296);
        var target=Math.Clamp(36+(int)Math.Round((candidates-36)*density,MidpointRounding.AwayFromZero),36,candidates);
        Check(total==target,"World weapon density retained "+total+" locations, expected "+target);
        var guarantee=Math.Clamp(target/36,1,dlc?8:6);
        Check(pool.Keys.All(sid=>counts.GetValueOrDefault(sid)>=guarantee),
            "Guaranteed model copies below "+guarantee+" per regular weapon");
    }

    private static void CheckCompatibleWeapon(Dictionary<string,string[]> pool,string sid,
        string family,string tier,string path)
    {
        Check(pool.TryGetValue(sid,out var source),"World weapon assignment outside audited 36-model pool: "+sid);
        var wanted=family=="Pistol"?"Pistol":"Primary";
        var actual=source![1]=="Pistol"?"Pistol":"Primary";
        Check(wanted==actual && Math.Abs(Tier(source[2])-Tier(tier))<=1,
            "Incompatible world weapon family/tier "+sid+" at "+path);
    }

    private static void CheckCompatible(Dictionary<string,string[]> pool,string sid,
        string category,string tier,string path)
    {
        Check(pool.TryGetValue(sid,out var source),"Attachment outside 39-model audited pool: "+sid);
        Check(source![1]==category && Math.Abs(Tier(source[2])-Tier(tier))<=1,
            "World attachment wrong category/progression tier: "+path+" assigned "+sid);
    }
    private static int Tier(string raw)=>raw switch
    {
        "T1"=>1,"T2"=>2,"T3"=>3,"T4"=>4,
        _=>throw new InvalidDataException("Unexpected progression tier "+raw)
    };

    private static string ActorFile(string source,string level,string sid)
    {
        var normal=source.Replace('\\','/');
        var i=normal.LastIndexOf('/');
        var dir=normal[..i];
        var stem=Path.GetFileNameWithoutExtension(normal);
        return dir+"/"+stem+"/"+level+"/"+sid+".cfg";
    }

    // Parse full physical source path for both full prototype CFGs and
    // individual world actor files; report all nested assignments/removals
    // and cloned root refurl directives without discarding unused lines.
    private static Dictionary<string,Parsed> ReadOutput(string outputRoot)
    {
        var result=new Dictionary<string,Parsed>(StringComparer.Ordinal);
        var basePath=Path.Combine(outputRoot,"Stalker2","Content","GameLite");
        if(!Directory.Exists(basePath))return result;
        foreach(var file in Directory.GetFiles(basePath,"*.cfg",SearchOption.AllDirectories))
        {
            var physical=Path.GetRelativePath(basePath,file).Replace('\\','/');
            var source=physical;
            if(physical.EndsWith("_patch_TrueCustomDifficulty.cfg",StringComparison.Ordinal))
            {
                var tokens=physical.Split('/');
                Check(tokens.Length>=2,"Malformed production patch folder "+physical);
                var baseName=tokens[^1].Replace("_patch_TrueCustomDifficulty.cfg",".cfg");
                Check(tokens[^2]==Path.GetFileNameWithoutExtension(baseName),
                    "Wrong prototype BPatch output file layout "+physical);
                source=string.Join("/",tokens.Take(tokens.Length-2).Append(baseName));
            }
            var parsed=new Parsed();
            var stack=new List<string>();
            foreach(var raw in File.ReadAllText(file).Replace("\r","").Split('\n'))
            {
                var s=raw.Trim();
                if(s.Length==0 || s.StartsWith("//",StringComparison.Ordinal))continue;
                if(s=="struct.end")
                {
                    Check(stack.Count>0,"Unbalanced CFG struct.end "+physical);
                    stack.RemoveAt(stack.Count-1);continue;
                }
                var rm=Regex.Match(s,@"^(?<name>.+?)\s*:\s*removenode$");
                if(rm.Success)
                {
                    var path=string.Join("/",stack.Append(rm.Groups["name"].Value));
                    Check(parsed.Removed.Add(path),"Duplicate removal node "+physical+"|"+path);
                    continue;
                }
                var open=Regex.Match(s,@"^(?<name>.+?)\s*:\s*struct\.begin(?<flags>.*)$");
                if(open.Success)
                {
                    var name=open.Groups["name"].Value;
                    var flags=open.Groups["flags"].Value.Trim();
                    Check(flags.Length==0 || flags.Contains("{bpatch}",StringComparison.Ordinal)
                        || flags.Contains("{refurl=",StringComparison.Ordinal),
                        "Unrecognized struct directive "+s);
                    var path=string.Join("/",stack.Append(name));
                    Check(parsed.Nodes.Add(path),"Duplicate CFG patch node "+physical+"|"+path);
                    if(flags.StartsWith("{refurl=",StringComparison.Ordinal))
                    {
                        Check(stack.Count==0,"Only a referenced clone root may contain refurl");
                        Check(parsed.References.TryAdd(path,flags.Trim('{','}')),
                            "Duplicate referenced clone root "+path);
                    }
                    stack.Add(name);continue;
                }
                var assignment=Regex.Match(s,@"^(?<name>\S+)\s*=\s*(?<value>.*)$");
                Check(assignment.Success && stack.Count>0,"Unexpected world CFG syntax "+s);
                var key=string.Join("/",stack)+"/"+assignment.Groups["name"].Value;
                Check(parsed.Fields.TryAdd(key,assignment.Groups["value"].Value),
                    "Duplicate physical world BPatch field "+source+"|"+key);
            }
            Check(stack.Count==0,"Unclosed world CFG patch "+physical);
            Check(result.TryAdd(source,parsed),"Two output files target same original CFG source "+source);
        }
        return result;
    }

    private static List<string[]> Csv(string file,int columns)
    {
        var dir=new DirectoryInfo(Directory.GetCurrentDirectory());
        while(dir!=null)
        {
            var p=Path.Combine(dir.FullName,"src","TrueCustomDifficulty","Data",file);
            if(File.Exists(p))
            {
                var result=new List<string[]>();
                foreach(var row in File.ReadLines(p).Skip(1).Where(x=>x.Length>0))
                {
                    var cells=row.Split(',');
                    Check(cells.Length==columns,file+": CSV columns drift");
                    result.Add(cells);
                }
                return result;
            }
            dir=dir.Parent;
        }
        throw new FileNotFoundException("Production world placement CSV missing "+file);
    }

    private static void Check(bool valid,string error)
    {
        if(!valid)throw new InvalidDataException(error);
    }
}
