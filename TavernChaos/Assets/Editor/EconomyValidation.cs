using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace TavernChaos.Editor
{
    public static class EconomyValidation
    {
        [Serializable] private sealed class Report { public bool passed; public List<string> checks=new(),failures=new(); }
        public static void EnsureSettings()
        {
            const string path="Assets/Resources/EconomySettings.asset";
            var settings=AssetDatabase.LoadAssetAtPath<EconomySettings>(path);
            if(!settings) { Directory.CreateDirectory("Assets/Resources"); AssetDatabase.Refresh(); settings=ScriptableObject.CreateInstance<EconomySettings>(); AssetDatabase.CreateAsset(settings,path); AssetDatabase.SaveAssets(); }
            if(!settings.Valid) throw new Exception("EconomySettings contains invalid prices or starting chips.");
        }
        public static void Run()
        {
            EnsureSettings(); var report=new Report(); SessionLedgerChecks.Run((ok,name)=>{(ok?report.checks:report.failures).Add(name);});
            report.passed=report.failures.Count==0; Directory.CreateDirectory("../Evidence/economy"); File.WriteAllText("../Evidence/economy/ledger.json",JsonUtility.ToJson(report,true));
            if(!report.passed) throw new Exception(string.Join("\n",report.failures)); Debug.Log("ECONOMY_CHECKS_OK: "+report.checks.Count);
        }
    }
}
