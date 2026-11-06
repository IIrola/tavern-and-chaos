using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace TavernChaos.Editor
{
    public static class BlackjackValidation
    {
        [Serializable] private sealed class Report { public bool passed; public List<string> checks=new(),failures=new(); }
        [MenuItem("Tavern/Validate blackjack rules")]
        public static void Run()
        {
            var report=new Report(); BlackjackChecks.Run((ok,name)=>(ok?report.checks:report.failures).Add(name));
            report.passed=report.failures.Count==0; Directory.CreateDirectory("../Evidence/blackjack"); File.WriteAllText("../Evidence/blackjack/rules.json",JsonUtility.ToJson(report,true));
            if(!report.passed) throw new Exception(string.Join("\n",report.failures)); Debug.Log("BLACKJACK_CHECKS_OK: "+report.checks.Count);
        }
    }
}
