using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace TavernChaos.Editor
{
    public static class PoolRulesValidation
    {
        [Serializable] private sealed class Report { public bool passed; public List<string> checks=new(),failures=new(); }
        public static void Run()
        {
            var report=new Report(); EightBallChecks.Run((ok,name)=>{ (ok?report.checks:report.failures).Add(name); });
            report.passed=report.failures.Count==0; Directory.CreateDirectory("../Evidence/economy");
            File.WriteAllText("../Evidence/economy/pool-rules.json",JsonUtility.ToJson(report,true));
            if(!report.passed) throw new Exception(string.Join("\n",report.failures));
            Debug.Log("POOL_RULES_OK: "+report.checks.Count);
        }
    }
}
