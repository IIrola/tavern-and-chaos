using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace TavernChaos.Editor
{
    public static class SessionValidation
    {
        [Serializable] private sealed class Report { public bool passed; public List<string> checks=new(),failures=new(); }
        public static void Run()
        {
            var report=new Report(); RoomLifecycleChecks.Run((ok,name)=>{(ok?report.checks:report.failures).Add(name);});
            report.passed=report.failures.Count==0; Directory.CreateDirectory("../Evidence/armwrestling");
            File.WriteAllText("../Evidence/armwrestling/lifecycle.json",JsonUtility.ToJson(report,true));
            if(!report.passed) throw new Exception(string.Join("\n",report.failures)); Debug.Log("SESSION_CHECKS_OK: "+report.checks.Count);
        }
    }
}
