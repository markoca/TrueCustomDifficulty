using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

// Phase 14B1: real production writer, cloned template bodies, original
// redirect CSV fanouts, and exact source-level control ownership.
// World actor-removal/shuffle writers have their own separate acceptance gate.
internal static class Program
{
    static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();
    static readonly string[] ScalarIds =
    {
        "loot.npcMedicalChance",
        "loot.cheapStashAmmoQuantity", "loot.cheapStashGrenadeChance",
        "loot.cheapStashGrenadeQuantity", "loot.mediumStashAmmoQuantity",
        "loot.expensiveStashAmmoQuantity", "loot.destructibleAmmoQuantity",
        "loot.destructibleGrenadeQuantity", "loot.ammoBoxWeight918",
        "loot.ammoBoxWeight045", "loot.ammoBoxWeight545",
        "loot.ammoBoxWeight012"
    };
    static readonly JsonElement Special = LoadSpecial();
    static readonly string MainFile = "GameData/ItemGeneratorPrototypes.cfg";
    static int Passed;
    static string? Keep;

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            if(args.Length!=0)
            {
                if(args.Length!=2 || args[0]!="--out")throw new ArgumentException("Use [--out PATH]");
                Keep=Path.GetFullPath(args[1]);Directory.CreateDirectory(Keep);
            }
            Inventory();
            foreach(var d in new[]{"Novice","Stalker","Veteran","Master"})
                Run(d,false,new(),"vanilla");
            foreach(var id in ScalarIds)
            {
                var par=Get("Veteran",id);
                var lo=id=="loot.cheapStashGrenadeQuantity" ? 0.0 :
                    id=="loot.cheapStashGrenadeChance" || id.StartsWith("loot.ammoBoxWeight",StringComparison.Ordinal)
                    || id=="loot.npcMedicalChance" ? 0.0 : par.Minimum;
                var hi=par.Maximum;
                foreach(var dlc in new[]{false,true})
                {
                    Run("Veteran",dlc,new(){[id]=lo},"low-"+id);
                    Run("Veteran",dlc,new(){[id]=hi},"high-"+id);
                }
            }
            // Aggregate anchors and other original CFG domains must never
            // trigger unedited special clones or duplicate redirect sets.
            Run("Veteran",false,new() {
                ["loot.cheapStashAmmoQuantity"]=0.5,
                ["loot.cheapStashGrenadeChance"]=0.7,
                ["loot.cheapStashGrenadeQuantity"]=3.0
            },"cheap-triple");
            Run("Veteran",true,new() {
                ["loot.destructibleAmmoQuantity"]=1.5,
                ["loot.ammoBoxWeight918"]=0.5,
                ["loot.ammoBoxWeight545"]=2.0,
                ["loot.destructibleGrenadeQuantity"]=1.5
            },"breakable-mixed-dlc");
            Run("Veteran",false,new() {
                ["loot.mediumStashAmmoQuantity"]=0.5,
                ["loot.expensiveStashAmmoQuantity"]=2.0,
                ["loot.npcMedicalChance"]=0.5
            },"medium-rare-medic");
            Run("Veteran",true,new() {
                ["loot.cheapStashAmmoQuantity"]=1.5,
                ["loot.mediumStashAmmoQuantity"]=1.5,
                ["loot.expensiveStashAmmoQuantity"]=1.5
            },"all-stash-dlc");
            // The source-only visual anchor bug found in 14A must stay fixed
            // even when multiple special generator families share one file.
            Run("Veteran",false,new() {
                ["loot.ammoBoxWeight045"]=0.0,
                ["loot.ammoBoxWeight012"]=2.0,
                ["loot.destructibleAmmoQuantity"]=2.0
            },"ammo-distribution");
            // Corpse package can be switched; ammo counts only matter when the
            // configured smart loot package contains NPC_Ammo_Smart.
            Run("Novice",false,new(),"corpse-none","");  // original Novice includes ammo
            Run("Stalker",false,new(),"corpse-medicine","NPC_Medicine_Smart, NPC_Ammo_Smart");
            Run("Veteran",false,new(),"corpse-water","NPC_Water_Smart");
            Run("Novice",false,new(){["loot.npcWeaponAmmoSupply"]=0.5},"corpse-ammo-present");
            Run("Novice",true,new(){["loot.npcWeaponAmmoSupply"]=2.0},"corpse-ammo-present-dlc");
            Run("Veteran",false,new(){["loot.npcWeaponAmmoSupply"]=2.0},"corpse-ammo-absent");
            Check(Passed==63,"Expected 63 special Loot scenarios, got "+Passed);
            Console.WriteLine($"PASS: {Passed} Loot SPECIAL generator and corpse writer scenarios");
            Console.WriteLine("NOT VERIFIED: original decoded CFG source values, world shuffle/removal writers, Unreal patch loading or gameplay");
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("FAIL: "+ex);return 1;}
    }

    static JsonElement LoadSpecial()
    {
        var dir=new DirectoryInfo(Directory.GetCurrentDirectory());
        while(dir!=null)
        {
            var p=Path.Combine(dir.FullName,"src","TrueCustomDifficulty","Data","cfg-writer-baseline.json");
            if(File.Exists(p))return JsonDocument.Parse(File.ReadAllText(p)).RootElement.GetProperty("special");
            dir=dir.Parent;
        }
        throw new FileNotFoundException("Original writer manifest file is required by standalone regression tests");
    }

    static ParameterViewModel Get(string d,string id)=>new MainViewModel(Catalog)
    {SelectedReferenceDifficulty=d}.AllParameters.Single(x=>x.Id==id);

    static void Inventory()
    {
        var defs=Catalog.Parameters.Where(p=>p.Id.StartsWith("loot.",StringComparison.Ordinal)).ToArray();
        var direct=Baseline.EditPoints.Where(p=>p.ParameterId.StartsWith("loot.",StringComparison.Ordinal))
            .Select(p=>p.ParameterId).ToHashSet(StringComparer.Ordinal);
        var specials=defs.Where(x=>!direct.Contains(x.Id)).Select(x=>x.Id).ToHashSet(StringComparer.Ordinal);
        var ours=ScalarIds.Concat(new[]{"loot.npcCorpseBonus","loot.npcWeaponAmmoSupply"})
            .ToHashSet(StringComparer.Ordinal);
        Check(defs.Length==40 && direct.Count==16 && specials.Count==24
            && ours.IsSubsetOf(specials),"Loot taxonomy changed: 16 direct, 24 special, 14 selected generator inputs");
        var mounted=new MainViewModel(Catalog).AllParameters;
        Check(ours.All(id=>mounted.Any(x=>x.Id==id && x.IsControlEnabled)),
            "A generator source is not in the actual ViewModel");
        var c=Special.GetProperty("cheapStashProduction");
        var m=Special.GetProperty("mediumStashAmmoProduction");
        var e=Special.GetProperty("expensiveStashProduction");
        var med=Special.GetProperty("npcMedicalLootProduction");
        var a=Special.GetProperty("destructibleAmmoProduction");
        var g=Special.GetProperty("destructibleGrenadeProduction");
        Check(c.GetProperty("templateCategories").GetArrayLength()==12 &&
            c.GetProperty("ammoEntries").GetArrayLength()==3 &&
            m.GetProperty("ammoEntries").GetArrayLength()==25 &&
            e.GetProperty("ammoEntries").GetArrayLength()==25 &&
            e.GetProperty("couponEntries").GetArrayLength()==8 &&
            med.GetProperty("pools").GetArrayLength()==6 &&
            a.GetProperty("pools").GetArrayLength()==2 &&
            g.GetProperty("pools").GetArrayLength()==2 &&
            Special.GetProperty("npcCorpseAmmoProduction").GetProperty("entries").GetArrayLength()==43,
            "Audited Loot production template source inventories drift");
        foreach(var (config,count) in new[]{(c,0),(m,450),(e,305),(med,33),(a,1235),(g,182)})
        {
            var rows=Redirects(config,includeDlc:true).ToArray();
            if(count!=0)Check(rows.Length==count,"Embedded production redirect manifest drift "+count+" / "+rows.Length);
        }
    }

    static void Run(string difficulty,bool dlc,Dictionary<string,double> changes,
                    string label,string? corpseChoice=null)
    {
        var dir=Path.Combine(Keep??Path.GetTempPath(),"TCD_LootSpecial_Contract",Passed+"-"+label+(dlc?"-DLC1":""));
        try
        {
            var vm=new MainViewModel(Catalog){SelectedReferenceDifficulty=difficulty};
            foreach(var (id,target) in changes)
            {
                var par=vm.AllParameters.Single(x=>x.Id==id);
                Check(par.IsSlider && par.IsControlEnabled && !par.IsValueModifiedFromReference(),
                      label+": not a live vanilla source "+id);
                par.SnapCurrentNumberToMarker(target);
                Check(Math.Abs(par.CurrentNumber-target)<0.0000001 &&
                      par.IsValueModifiedFromReference(),label+": source did not change "+id);
            }
            if(corpseChoice!=null)
            {
                var corpse=vm.AllParameters.Single(x=>x.Id=="loot.npcCorpseBonus");
                Check(corpse.IsChoiceSlider && corpse.ChoiceOptions.Any(x=>x.Value==corpseChoice),
                    "Invalid corpse smart-loot option "+corpseChoice);
                corpse.CurrentText=corpseChoice;
                Check(corpse.IsValueModifiedFromReference(),"Corpse switch failed");
            }

            var changed=vm.AllParameters.Where(p=>p.Id.StartsWith("loot.",StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p=>p.Id).ToHashSet(StringComparer.Ordinal);
            var expectedIDs=changes.Keys.ToHashSet(StringComparer.Ordinal);
            if(corpseChoice!=null)expectedIDs.Add("loot.npcCorpseBonus");
            Check(changed.SetEquals(expectedIDs),"Unexpected unrelated Loot changes: "+string.Join(",",changed));
            // Only source-level modifications can activate special generators.
            var actual=new CfgPatchWriterService().Write(vm,Catalog,
                dlc?new GameInstallInfo{HasDlc1=true}:null,dir);
            var paths=ReadOutput(dir);
            var expected=new Dictionary<string,string>(StringComparer.Ordinal);
            var refs=new Dictionary<string,string>(StringComparer.Ordinal);
            void Add(string file,string path,string value)
            {
                var key=file+"|"+path;
                Check(expected.TryAdd(key,value),"Expected special CFG leaf collision: "+key);
            }
            void Generator(string configName,double quantity,double? chance=null,double? grenadeQty=null)
            {
                var config=Special.GetProperty(configName);
                var file=Str(config,"file");
                var roots=new[]{Str(config,"customGeneratorSid")}
                    .Concat(config.GetProperty("rankWrappers").EnumerateArray()
                        .Select(r=>Str(r,"targetSid"))).ToArray();
                foreach(var root in roots)
                {
                    refs[file+"|"+root]="refurl="+Str(config,"baseTemplateRefUrl")
                        +";refkey="+Str(config,"baseTemplateRefKey");
                    Add(file,root+"/SID",root);
                    foreach(var category in config.GetProperty("templateCategories").EnumerateArray())
                    {
                        var cat=Str(category,"node");
                        foreach(var field in category.GetProperty("fields").EnumerateObject())
                            Add(file,root+"/ItemGenerator/"+cat+"/"+field.Name,field.Value.GetString()!);
                        foreach(var item in category.GetProperty("items").EnumerateArray())
                        {
                            var entry=Str(item,"node");
                            foreach(var field in item.GetProperty("fields").EnumerateObject())
                            {
                                var val=field.Value.GetString()!;
                                if(field.Name is "MinCount" or "MaxCount")
                                {
                                    var sourceEntry=category.GetProperty("node").GetString();
                                    var ammo=(configName=="cheapStashProduction"
                                        ? cat=="[3]" && config.GetProperty("ammoEntries").EnumerateArray()
                                            .Any(x=>Str(x,"entryNode")==entry)
                                        : config.GetProperty("ammoEntries").EnumerateArray().Any(x=>
                                            Str(x,"categoryNode")==cat && Str(x,"entryNode")==entry));
                                    if(ammo)
                                    {
                                        var rec=config.GetProperty("ammoEntries").EnumerateArray().First(x=>
                                            Str(x,"entryNode")==entry &&
                                            (configName=="cheapStashProduction" || Str(x,"categoryNode")==cat));
                                        val=HalfUpPositive(rec.GetProperty(field.Name=="MinCount"?"sourceMinCount":"sourceMaxCount").GetInt32(),quantity).ToString(CultureInfo.InvariantCulture);
                                    }
                                    if(configName=="cheapStashProduction" && grenadeQty.HasValue
                                        && cat==Str(config.GetProperty("grenadeEntry"),"categoryNode")
                                        && entry==Str(config.GetProperty("grenadeEntry"),"entryNode"))
                                        val=grenadeQty.Value.ToString("0",CultureInfo.InvariantCulture);
                                }
                                else if(field.Name=="Chance" && configName=="cheapStashProduction"
                                    && chance.HasValue && cat==Str(config.GetProperty("grenadeEntry"),"categoryNode")
                                    && entry==Str(config.GetProperty("grenadeEntry"),"entryNode"))
                                    val=chance.Value.ToString("0.###",CultureInfo.InvariantCulture);
                                else if(field.Name=="Chance" && configName=="expensiveStashProduction"
                                    && chance.HasValue && config.GetProperty("couponEntries").EnumerateArray().Any(x=>
                                        Str(x,"categoryNode")==cat && Str(x,"entryNode")==entry
                                        && x.GetProperty("difficultySids").EnumerateArray()
                                           .Any(d=>d.GetString()==Catalog.DifficultyProfiles[difficulty].Sid)))
                                    val=chance.Value.ToString("0.###",CultureInfo.InvariantCulture);
                                Add(file,root+"/ItemGenerator/"+cat+"/PossibleItems/"+entry+"/"+field.Name,val);
                            }
                        }
                    }
                }
                foreach(var row in Redirects(config,dlc))
                    Add(row.File,row.Prototype+"/"+row.NodePath.Replace(".","/")+"/"+row.Key,row.Target);
            }
            void Medical(double multiplier)
            {
                var config=Special.GetProperty("npcMedicalLootProduction");
                var file=Str(config,"file");
                foreach(var pool in config.GetProperty("pools").EnumerateArray())
                {
                    var root=Str(pool,"customSid");
                    refs[file+"|"+root]="refurl="+Str(config,"baseTemplateRefUrl")
                        +";refkey="+Str(config,"baseTemplateRefKey");
                    Add(file,root+"/SID",root);
                    foreach(var category in pool.GetProperty("templateCategories").EnumerateArray())
                    {
                        var cat=Str(category,"node");
                        foreach(var field in category.GetProperty("fields").EnumerateObject())
                            Add(file,root+"/ItemGenerator/"+cat+"/"+field.Name,field.Value.GetString()!);
                        foreach(var item in category.GetProperty("items").EnumerateArray())
                        {
                            var entry=Str(item,"node");
                            foreach(var field in item.GetProperty("fields").EnumerateObject())
                            {
                                var value=field.Value.GetString()!;
                                if(field.Name=="Chance")
                                {
                                    var src=pool.GetProperty("medicalEntries").EnumerateArray()
                                        .Where(x=>Str(x,"categoryNode")==cat && Str(x,"entryNode")==entry)
                                        .ToArray();
                                    if(src.Length==1)value=Math.Clamp(Num(Str(src[0],"sourceChanceRaw"))*multiplier,0,1)
                                        .ToString("0.###",CultureInfo.InvariantCulture);
                                }
                                Add(file,root+"/ItemGenerator/"+cat+"/PossibleItems/"+entry+"/"+field.Name,value);
                            }
                        }
                    }
                }
                foreach(var row in Redirects(config,dlc))
                    Add(row.File,row.Prototype+"/"+row.NodePath.Replace(".","/")+"/"+row.Key,row.Target);
            }
            void Boxes(string configName,double quantity)
            {
                var config=Special.GetProperty(configName);
                var file=Str(config,"file");
                foreach(var pool in config.GetProperty("pools").EnumerateArray())
                {
                    var root=Str(pool,"targetSid");
                    refs[file+"|"+root]="refurl="+Str(config,"baseTemplateRefUrl")
                        +";refkey="+Str(config,"baseTemplateRefKey");
                    Add(file,root+"/SID",root);
                    var cat=Str(pool,"categoryNode");
                    foreach(var field in pool.GetProperty("categoryFields").EnumerateObject())
                        Add(file,root+"/ItemGenerator/"+cat+"/"+field.Name,field.Value.GetString()!);
                    foreach(var item in pool.GetProperty("items").EnumerateArray())
                    {
                        var entry=Str(item,"node");
                        foreach(var field in item.GetProperty("fields").EnumerateObject())
                        {
                            var value=field.Value.GetString()!;
                            if(field.Name is "MinCount" or "MaxCount")
                                value=HalfUpPositive(item.GetProperty(field.Name=="MinCount"?"sourceMinCount":"sourceMaxCount").GetInt32(),quantity)
                                    .ToString(CultureInfo.InvariantCulture);
                            if(field.Name=="Weight" && configName=="destructibleAmmoProduction")
                            {
                                var sid=Str(item.GetProperty("fields"),"ItemPrototypeSID");
                                var parameterId=Str(config.GetProperty("weightParameterIds"),sid);
                                var weight=changes.GetValueOrDefault(parameterId,1.0);
                                value=(Num(value)*weight).ToString("0.###",CultureInfo.InvariantCulture);
                            }
                            Add(file,root+"/ItemGenerator/"+cat+"/PossibleItems/"+entry+"/"+field.Name,value);
                        }
                    }
                }
                foreach(var row in Redirects(config,dlc))
                    Add(row.File,row.Prototype+"/"+row.NodePath.Replace(".","/")+"/"+row.Key,row.Target);
            }

            var cheapIDs=new[]{"loot.cheapStashAmmoQuantity","loot.cheapStashGrenadeChance","loot.cheapStashGrenadeQuantity"};
            if(cheapIDs.Any(changes.ContainsKey))
                Generator("cheapStashProduction",changes.GetValueOrDefault(cheapIDs[0],1),
                    changes.GetValueOrDefault(cheapIDs[1],0.1),
                    changes.GetValueOrDefault(cheapIDs[2],1));
            if(changes.ContainsKey("loot.mediumStashAmmoQuantity"))
                Generator("mediumStashAmmoProduction",changes["loot.mediumStashAmmoQuantity"]);
            if(changes.ContainsKey("loot.expensiveStashAmmoQuantity"))
                Generator("expensiveStashProduction",changes["loot.expensiveStashAmmoQuantity"]);
            if(changes.ContainsKey("loot.npcMedicalChance"))
                Medical(changes["loot.npcMedicalChance"]);
            var boxIDs=new[]{"loot.destructibleAmmoQuantity","loot.ammoBoxWeight918",
                "loot.ammoBoxWeight045","loot.ammoBoxWeight545","loot.ammoBoxWeight012"};
            if(boxIDs.Any(changes.ContainsKey))
                Boxes("destructibleAmmoProduction",changes.GetValueOrDefault(boxIDs[0],1));
            if(changes.ContainsKey("loot.destructibleGrenadeQuantity"))
                Boxes("destructibleGrenadeProduction",changes["loot.destructibleGrenadeQuantity"]);

            // Corpse choice is difficulty-specific and intentionally replaces
            // only EconomyDifficulty.CorpseSmartLoot rather than vanilla NPC pools.
            if(corpseChoice!=null)
            {
                var sid=Catalog.DifficultyProfiles[difficulty].Sid;
                var first="GameData/DifficultyPrototypes.cfg|"+sid+"/EconomyDifficulty/CorpseSmartLoot";
                if(corpseChoice.Length==0)Add("GameData/DifficultyPrototypes.cfg",sid+"/EconomyDifficulty/CorpseSmartLoot","");
                else Add("GameData/DifficultyPrototypes.cfg",sid+"/EconomyDifficulty/CorpseSmartLoot/GeneralNPC",corpseChoice);
            }

            if(changes.ContainsKey("loot.npcWeaponAmmoSupply") &&
                vm.AllParameters.Single(p=>p.Id=="loot.npcCorpseBonus").CurrentText
                    .Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)
                    .Contains("NPC_Ammo_Smart",StringComparer.Ordinal))
            {
                var config=Special.GetProperty("npcCorpseAmmoProduction");
                var root=Str(config,"targetSid");
                var file=Str(config,"file");
                refs[file+"|"+root]="refurl="+Str(config,"baseTemplateRefUrl")
                    +";refkey="+Str(config,"baseTemplateRefKey");
                Add(file,root+"/SID",root);
                var multiplier=changes["loot.npcWeaponAmmoSupply"];
                foreach(var entry in config.GetProperty("entries").EnumerateArray())
                {
                    var path=root+"/ItemGenerators/["+entry.GetProperty("rankIndex").GetInt32()+"]/SmartLootParams/PrimaryWeaponParams/["
                        +entry.GetProperty("groupIndex").GetInt32()+"]/Items/["+entry.GetProperty("itemIndex").GetInt32()+"]";
                    Add(file,path+"/MinCount",HalfUpZero(entry.GetProperty("minCount").GetInt32(),multiplier).ToString(CultureInfo.InvariantCulture));
                    Add(file,path+"/MaxCount",HalfUpZero(entry.GetProperty("maxCount").GetInt32(),multiplier).ToString(CultureInfo.InvariantCulture));
                }
                var sid=Catalog.DifficultyProfiles[difficulty].Sid;
                var packages=vm.AllParameters.Single(p=>p.Id=="loot.npcCorpseBonus").CurrentText
                    .Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
                var altered=string.Join(", ",packages.Select(x=>x=="NPC_Ammo_Smart"?root:x));
                Add("GameData/DifficultyPrototypes.cfg",sid+"/EconomyDifficulty/CorpseSmartLoot/GeneralNPC",altered);
            }
            Check(paths.Values.SelectMany(x=>x.Keys).Count()==expected.Count,
                label+": unexpected number of production assignments observed "
                +paths.Values.SelectMany(x=>x.Keys).Count()+" expected "+expected.Count);
            foreach(var (key,expectedValue) in expected)
            {
                var parts=key.Split('|',2);
                Check(paths.TryGetValue(parts[0],out var doc),
                    label+": missing CFG document "+parts[0]);
                Check(doc!.TryGetValue(parts[1],out var actualVal),
                    label+": missing CFG field "+key);
                if(double.TryParse(expectedValue,CultureInfo.InvariantCulture,out var expectedNum)
                    && double.TryParse(actualVal,CultureInfo.InvariantCulture,out var actualNum))
                    Check(Math.Abs(expectedNum-actualNum)<0.0000011,label+": numeric mismatch "+key
                        +" expected="+expectedValue+" actual="+actualVal);
                else Check(string.Equals(expectedValue,actualVal,StringComparison.Ordinal),
                    label+": raw CFG text mismatch "+key+" expected="+expectedValue+" actual="+actualVal);
            }
            foreach(var file in paths)
                foreach(var leaf in file.Value.Keys)
                    Check(expected.ContainsKey(file.Key+"|"+leaf),label+": unrelated writer field "
                        +file.Key+"|"+leaf);
            foreach(var (key,refval) in refs)
            {
                var split=key.Split('|',2);
                var filePath=OutputPath(dir,split[0]);
                var text=File.ReadAllText(filePath);
                Check(text.Contains(split[1]+" : struct.begin",StringComparison.Ordinal)
                    && text.Contains(refval,StringComparison.Ordinal),
                    label+": missing clone/physical refurl root: "+key);
            }
            Check(actual.FileCount==paths.Count && actual.ParameterCount==expectedIDs.Count,
                label+": writer file/control counts disagree with source modifications: "
                +actual.FileCount+"/"+actual.ParameterCount+"/"+actual.EditCount);
            if(corpseChoice==null && !changes.ContainsKey("loot.npcWeaponAmmoSupply"))
                Check(actual.EditCount==expected.Count,label+": one emitted assignment per intended field drift");
            Console.WriteLine("PASS "+difficulty+" "+label+(dlc?" +DLC1":" BASE")
                +" ("+paths.Count+" files / "+expected.Count+" exact assigned fields)");
            Passed++;
        }
        finally{if(Keep==null && Directory.Exists(dir))Directory.Delete(dir,true);}
    }

    static JsonElement StrGet(JsonElement obj,string key)=>obj.GetProperty(key);
    static string Str(JsonElement obj,string key)=>obj.GetProperty(key).GetString()!;
    static int HalfUpPositive(int source,double scale)=>Math.Max(1,(int)Math.Floor(source*scale+0.5));
    static int HalfUpZero(int source,double scale)=>Math.Max(0,(int)Math.Floor(source*scale+0.5));
    static double Num(string s)=>double.Parse(s.TrimEnd('f','F'),CultureInfo.InvariantCulture);

    readonly record struct Redirect(string File,string Prototype,string NodePath,string Key,string Target);
    static IEnumerable<Redirect> Redirects(JsonElement config,bool includeDlc)
    {
        if(!config.TryGetProperty("redirectManifestResource",out var prop))
            yield break;
        var resource=prop.GetString()!;
        var file=resource.Replace("TrueCustomDifficulty.Data.","",StringComparison.Ordinal);
        var dir=new DirectoryInfo(Directory.GetCurrentDirectory());
        while(dir!=null && !File.Exists(Path.Combine(dir.FullName,"src","TrueCustomDifficulty","Data",file)))
            dir=dir.Parent;
        if(dir==null)throw new FileNotFoundException("Missing real production redirect CSV "+file);
        var path=Path.Combine(dir.FullName,"src","TrueCustomDifficulty","Data",file);
        var lines=File.ReadAllLines(path);
        foreach(var line in lines.Skip(1).Where(x=>!string.IsNullOrWhiteSpace(x)))
        {
            var c=line.Split(',');
            Check(c.Length>=7,"Wrong production CSV columns "+file);
            if(!includeDlc && c[1].StartsWith("DLCGameData/",StringComparison.OrdinalIgnoreCase))
                continue;
            yield return new Redirect(c[1].Trim(),c[2].Trim(),c[3].Trim(),c[4].Trim(),c[6].Trim());
        }
    }

    static string OutputPath(string root,string source)
    {
        var file=Path.GetFileName(source);
        var stem=Path.GetFileNameWithoutExtension(file);
        var parent=Path.GetDirectoryName(source.Replace('/',Path.DirectorySeparatorChar))??"";
        var folder=Path.Combine(root,"Stalker2","Content","GameLite",parent);
        return file.EndsWith("Prototypes.cfg",StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(folder,stem,stem+"_patch_TrueCustomDifficulty.cfg")
            : Path.Combine(folder,file+"_patch_TrueCustomDifficulty");
    }

    // No naive text substring check: each generated file is parsed into
    // concrete full-path leaf identities, retaining clone and bpatch nesting.
    static Dictionary<string,Dictionary<string,string>> ReadOutput(string folder)
    {
        var docs=new Dictionary<string,Dictionary<string,string>>(StringComparer.Ordinal);
        foreach(var path in Directory.GetFiles(folder,"*TrueCustomDifficulty*",SearchOption.AllDirectories))
        {
            var full=path.Replace('\\','/');
            var start=full.IndexOf("/Stalker2/Content/GameLite/",StringComparison.Ordinal);
            Check(start>=0,"Unknown output layout "+full);
            var rel=full[(start+"/Stalker2/Content/GameLite/".Length)..];
            var segments=rel.Split('/');
            string file;
            if(rel.EndsWith("_patch_TrueCustomDifficulty.cfg",StringComparison.Ordinal)
                && segments.Length>=2 && segments[^2]==segments[^1].Replace("_patch_TrueCustomDifficulty.cfg",""))
            {
                var f=segments[^1].Replace("_patch_TrueCustomDifficulty.cfg",".cfg");
                file=string.Join("/",segments.Take(segments.Length-2).Append(f));
            }
            else file=rel.Replace("_patch_TrueCustomDifficulty","");
            var fields=new Dictionary<string,string>(StringComparer.Ordinal);
            var stack=new List<string>();
            foreach(var original in File.ReadAllText(path).Replace("\r","").Split('\n'))
            {
                var line=original.Trim();
                if(line.Length==0 || line.StartsWith("//",StringComparison.Ordinal))continue;
                if(line=="struct.end")
                {
                    Check(stack.Count>0,"Extra struct.end "+file);
                    stack.RemoveAt(stack.Count-1);continue;
                }
                var open=Regex.Match(line,@"^(?<name>\S+)\s*:\s*struct\.begin(?<extra>.*)$");
                if(open.Success)
                {
                    var name=open.Groups["name"].Value;
                    var extra=open.Groups["extra"].Value;
                    Check(stack.Count!=0 || extra.Contains("{bpatch}",StringComparison.Ordinal)
                        || extra.Contains("{refurl=",StringComparison.Ordinal),
                        "Top-level root is neither patch nor referenced clone: "+line);
                    stack.Add(name);continue;
                }
                var kv=Regex.Match(line,@"^(?<name>\S+)\s*=\s*(?<value>.*)$");
                Check(kv.Success && stack.Count>0,"Unparsed special CFG syntax: "+line);
                Check(fields.TryAdd(string.Join("/",stack)+"/"+kv.Groups["name"].Value,
                    kv.Groups["value"].Value),"Duplicate output field "+file);
            }
            Check(stack.Count==0,"Unclosed CFG doc "+file);
            Check(docs.TryAdd(file,fields),"Duplicate emitted physical CFG "+file);
        }
        return docs;
    }
    static void Check(bool valid,string msg)
    {
        if(!valid)throw new InvalidDataException(msg);
    }
}
