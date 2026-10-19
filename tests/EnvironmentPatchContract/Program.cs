using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace EnvironmentPatchContract;

// Executes actual ViewModel and production BPatch writer; does not execute UE.
internal static class Program
{
    static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();
    static readonly string[] Diffs = { "Novice", "Stalker", "Veteran", "Master" };
    static string? Keep;
    static int Passed;

    [STAThread] static int Main(string[] args)
    {
        try
        {
            if (args.Length != 0)
            {
                if (args.Length != 2 || args[0] != "--out") throw new ArgumentException("Use [--out PATH]");
                Keep = Path.GetFullPath(args[1]); Directory.CreateDirectory(Keep);
            }
            var definitions = Catalog.Parameters.Where(p => p.Id.StartsWith("environment.", StringComparison.Ordinal))
                .OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
            var ids = definitions.Select(p => p.Id).ToArray();
            var vm = new MainViewModel(Catalog);
            var active = vm.AllParameters.Where(p => p.Id.StartsWith("environment.", StringComparison.Ordinal)).ToArray();
            var points = Baseline.EditPoints.Where(p => p.ParameterId.StartsWith("environment.", StringComparison.Ordinal)).ToArray();
            Check(ids.Length == 17 && active.Length == 17,
                "17 catalog/VM Environment sources; catalog=" + ids.Length +
                " VM=" + active.Length);
            var inaccessible = active.Where(p => !p.IsSlider || !p.IsControlEnabled)
                .Select(p => p.Id).ToArray();
            Check(inaccessible.Length == 0,
                "Environment sources unexpectedly disabled or not sliders: " +
                string.Join(", ", inaccessible));
            Check(active.Select(p => p.Id).ToHashSet(StringComparer.Ordinal).SetEquals(ids),
                "Environment catalog and VM source IDs disagree");
            // RazorAnomaly is the in-game Glass Shards hazard. Keep actual
            // CFG prototype IDs stable but expose player-facing wording.
            Check(definitions.Single(p => p.Id == "environment.razorHealthDamage").Label ==
                "Glass Shards Health Damage", "Glass Shards damage title");
            Check(definitions.Single(p => p.Id == "environment.razorBleeding").Label ==
                "Glass Shards Bleeding", "Glass Shards bleeding title");

            // Diamond has a real CFG prototype but no verified encounter.
            // Its legacy parameter/writer remains for old presets, not for UI.
            foreach (var diamondId in new[] {
                "environment.diamondHealthDamage", "environment.diamondBleeding"
            })
            {
                var parameter = active.Single(p => p.Id == diamondId);
                Check(!parameter.Definition.ExposeInUi && !parameter.IsControlVisible &&
                    !parameter.IsIndividualAnomalyDamageMember &&
                    !parameter.IsVisualAggregateMember, "Diamond must be hidden: " + diamondId);
            }
            Check(active.Single(p => p.Id == "environment.razorHealthDamage")
                .IsIndividualAnomalyDamageMember, "Glass Shards remains in anomaly damage editor");
            Check(active.Single(p => p.Id == "environment.razorBleeding")
                .IsVisualAggregateMember, "Glass Shards bleeding retains its visual family");
            // Two same 0..2x/0.01x source-native knobs: UI changes
            // must not change their existing parameter precision or writers.
            foreach (var id in new[] {
                "environment.razorBleeding", "environment.psyAnomalyIntensity"
            })
            {
                var knob = active.Single(p => p.Id == id);
                Check(knob.IsSlider && knob.IsControlEnabled &&
                    knob.Minimum == 0.0 && knob.Maximum == 2.0 &&
                    Math.Abs(knob.TickFrequency - 0.01) < 0.0000001,
                    "Source-native special-effect knob range/step: " + id);
            }
            Check(points.Length == 42 && points.All(p => p.Scope == "baseGame") &&
                points.Select(p => p.ParameterId).ToHashSet(StringComparer.Ordinal).SetEquals(ids), "42 base source fields");
            Check(points.All(p => p.Operation is "set" or "scale" or "setDeltaFromRetainedFraction"),
                "supported Environment operation inventory");
            var global = points.Where(p => p.ParameterId == "environment.anomalyDamage").ToArray();
            Check(global.Length == 4 && global.All(p => p.Key == "Anomaly_Damage" &&
                p.Operation == "set" && p.NodePath.SequenceEqual(new[]{"EnvironmentDifficulty"})) &&
                global.Select(p => p.DifficultySid).ToHashSet(StringComparer.Ordinal)
                    .SetEquals(Catalog.DifficultyProfiles.Values.Select(p => p.Sid)), "four global difficulty sources");
            var effects = points.Except(global).ToArray();
            Check(effects.Length == 38 &&
                effects.All(p => p.File == "GameData/EffectPrototypes.cfg" &&
                    p.NodePath.Count == 0 && p.Key is "ValueMin" or "ValueMax") &&
                effects.GroupBy(p => (p.ParameterId,p.Prototype)).All(g =>
                    g.Count() == 2 && g.Select(p => p.Key).ToHashSet(StringComparer.Ordinal)
                        .SetEquals(new[]{"ValueMin","ValueMax"})), "effect min/max pairs");
            Check(effects.Where(p => p.ParameterId is "environment.flycatcherMovementSpeed"
                or "environment.flycatcherTurnSpeed").All(p => p.Operation == "setDeltaFromRetainedFraction"),
                "Flycatcher signed delta operation");
            Check(effects.Where(p => p.ParameterId == "environment.psyAnomalyIntensity")
                .All(p => p.Operation == "scale" && Num(p.SourceRaw) < 0), "negative PSY source");
            foreach (var diff in Diffs) CheckCase(diff, new Dictionary<string,double>(), "vanilla");
            foreach (var id in ids) foreach (var diff in Diffs)
            {
                var p = new MainViewModel(Catalog) { SelectedReferenceDifficulty = diff }
                    .AllParameters.Single(x => x.Id == id);
                var step = Math.Max(0.01, p.Definition.Slider?.Step ?? 0.01);
                var candidate = p.ReferenceNumber + step <= p.Maximum
                    ? p.ReferenceNumber + step : p.ReferenceNumber - step;
                CheckCase(diff,new(){{id,candidate}},"single");
            }
            foreach (var (id,low,high) in new (string,double,double)[]
            {
                ("environment.anomalyDamage",0,2.5),
                ("environment.flycatcherMovementSpeed",0,1),
                ("environment.flycatcherTurnSpeed",0,1),
                ("environment.psyAnomalyIntensity",0,2),
                ("environment.diamondBleeding",0,20),
                ("environment.razorHealthDamage",0,2)
            })
            {
                CheckCase("Veteran",new(){{id,low}},"low");
                CheckCase("Veteran",new(){{id,high}},"high");
            }
            foreach (var pair in new[]
            {
                ("environment.anomalyDamage",1.8,"environment.electroAnomalyDamage",80.0),
                ("environment.flycatcherMovementSpeed",0.8,"environment.flycatcherTurnSpeed",0.4),
                ("environment.razorHealthDamage",0.6,"environment.razorBleeding",1.6),
                ("environment.razorBleeding",1.5,"environment.diamondBleeding",12.0),
                ("environment.psyAnomalyIntensity",1.2,"environment.diamondBleeding",11.0),
                ("environment.chemicalAnomalyDamage",40.0,"environment.fireAnomalyDamage",90.0)
            })
                CheckCase("Veteran",new(){{pair.Item1,pair.Item2},{pair.Item3,pair.Item4}},"pair");
            Check(Passed == 90,"exact 90 scenarios");
            Console.WriteLine($"PASS: {Passed} Environment production writer scenarios");
            Console.WriteLine("NOT VERIFIED: Unreal merge semantics or effects in game");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); return 1; }
    }

    static void CheckCase(string diff, Dictionary<string,double> values, string label)
    {
        var output = Path.Combine(Keep ?? Path.GetTempPath(),"TCD_Environment_Patch",
            diff+"-"+label+"-"+Passed.ToString(CultureInfo.InvariantCulture));
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = diff };
            foreach (var (id,value) in values)
            {
                var p=vm.AllParameters.Single(x=>x.Id==id);
                Check(p.IsSlider && p.IsControlEnabled && !p.IsValueModifiedFromReference(),"enabled vanilla slider "+id);
                p.SnapCurrentNumberToMarker(value);
                Check(Math.Abs(p.CurrentNumber-value)<0.0000011 && p.IsValueModifiedFromReference(),"edit real slider "+id);
            }
            var changedIds=vm.AllParameters.Where(p=>p.Id.StartsWith("environment.",StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p=>p.Id).ToHashSet(StringComparer.Ordinal);
            Check(changedIds.SetEquals(values.Keys),"collateral ViewModel changes "+label);
            var sid=Catalog.DifficultyProfiles[diff].Sid;
            var pars=vm.AllParameters.ToDictionary(p=>p.Id,StringComparer.Ordinal);
            var points=Baseline.EditPoints.Where(p=>values.ContainsKey(p.ParameterId)
                && (string.IsNullOrWhiteSpace(p.DifficultySid) || p.DifficultySid==sid))
                .Where(p=>Math.Abs(Expected(pars[p.ParameterId],p)-Num(p.SourceRaw))>=0.0000001)
                .ToArray();
            var writer=new CfgPatchWriterService().Write(vm,Catalog,null,output);
            var names=points.Select(p=>FilePath(output,p.File))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Check(writer.FileCount==names.Count && writer.EditCount==points.Length &&
                writer.ParameterCount==values.Count,"production writer counts "+diff+" "+label);
            var actual=Directory.GetFiles(output,"*TrueCustomDifficulty*",SearchOption.AllDirectories)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Check(actual.SetEquals(names),"writer emitted collateral CFG files "+diff+" "+label);
            foreach(var file in names)
            {
                var paths=ReadLeaves(File.ReadAllText(file));
                var expected=points.Where(p=>string.Equals(FilePath(output,p.File),file,
                    StringComparison.OrdinalIgnoreCase)).ToArray();
                var wanted=expected.Select(p=>p.Prototype+"/"
                    +(p.NodePath.Count>0?string.Join("/",p.NodePath)+"/":"")+p.Key)
                    .ToHashSet(StringComparer.Ordinal);
                Check(paths.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(wanted),
                    "unexpected/missing BPatch leaves "+diff+" "+label);
                foreach(var p in expected)
                {
                    var key=p.Prototype+"/"+(p.NodePath.Count>0?string.Join("/",p.NodePath)+"/":"")+p.Key;
                    Check(paths.TryGetValue(key,out var raw),"missing "+key);
                    Check(Math.Abs(Num(raw!)-Expected(pars[p.ParameterId],p))<0.0000011,
                        "wrong BPatch value "+key+" got "+raw);
                }
            }
            Passed++;
            Console.WriteLine("PASS "+diff+"-"+label+" ("+points.Length+" edits)");
        }
        finally { if(Keep is null && Directory.Exists(output)) Directory.Delete(output,true); }
    }

    static double Expected(ParameterViewModel p,CfgWriterEditPoint field)=>
        field.Operation switch
        {
            "set"=>p.CurrentNumber,
            "scale"=>Num(field.SourceRaw)*p.CurrentNumber,
            "setDeltaFromRetainedFraction"=>p.CurrentNumber-1.0,
            _=>throw new InvalidDataException("Unexpected environment operation")
        };

    static string FilePath(string root,string source)
    {
        var f=Path.GetFileName(source);var stem=Path.GetFileNameWithoutExtension(f);
        var dir=Path.Combine(root,"Stalker2","Content","GameLite",
            Path.GetDirectoryName(source.Replace('/',Path.DirectorySeparatorChar)) ?? "");
        return f.EndsWith("Prototypes.cfg",StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(dir,stem,stem+"_patch_TrueCustomDifficulty.cfg")
            : Path.Combine(dir,f+"_patch_TrueCustomDifficulty");
    }

    static double Num(string raw)
    {
        var s=raw.Trim();var percent=s.EndsWith("%",StringComparison.Ordinal);
        if(percent) s=s[..^1];
        if(s.EndsWith("f",StringComparison.OrdinalIgnoreCase)) s=s[..^1];
        return double.Parse(s,CultureInfo.InvariantCulture)/(percent?100:1);
    }

    // Reject missing bpatch tags at every nested level, unknown statements,
    // duplicate branches and any collateral assignments.
    static Dictionary<string,string> ReadLeaves(string cfg)
    {
        var leaves=new Dictionary<string,string>(StringComparer.Ordinal);
        var seen=new HashSet<string>(StringComparer.Ordinal);
        var stack=new List<string>();
        foreach(var raw in cfg.Replace("\r","").Split('\n'))
        {
            var s=raw.Trim();
            if(s.Length==0 || s.StartsWith("//",StringComparison.Ordinal)) continue;
            if(s=="struct.end")
            {
                Check(stack.Count>0,"unmatched struct.end");
                stack.RemoveAt(stack.Count-1);continue;
            }
            var open=Regex.Match(s,@"^(?<name>\S+)\s*:\s*struct\.begin(?<extra>.*)$");
            if(open.Success)
            {
                Check(open.Groups["extra"].Value.Contains("{bpatch}",StringComparison.Ordinal),
                    "missing bpatch marker: "+s);
                stack.Add(open.Groups["name"].Value);
                Check(seen.Add(string.Join("/",stack)),"duplicate BPatch prototype/node");
                continue;
            }
            var assignment=Regex.Match(s,@"^(?<key>\S+)\s*=\s*(?<value>.*)$");
            Check(assignment.Success && stack.Count>0,"unexpected CFG statement: "+s);
            var key=string.Join("/",stack)+"/"+assignment.Groups["key"].Value;
            Check(leaves.TryAdd(key,assignment.Groups["value"].Value),"duplicate CFG leaf: "+key);
        }
        Check(stack.Count==0,"unterminated BPatch document");
        return leaves;
    }
    static void Check(bool valid,string what) { if(!valid) throw new InvalidDataException(what); }
}
