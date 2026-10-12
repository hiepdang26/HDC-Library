using System;
using System.Collections.Generic;
#if !UNITY_EDITOR
using System.Reflection;
#endif

namespace HDC.Onboarding
{
    internal static class HDCFirebaseAnalytics
    {
        internal const string ParameterType = "Firebase.Analytics.Parameter, Firebase.Analytics";

        internal static Array Parameters(Type parameterType, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            Array values = Array.CreateInstance(parameterType, parameters.Count);
            for (int i = 0; i < parameters.Count; i++)
                values.SetValue(Activator.CreateInstance(parameterType, parameters[i].Key, parameters[i].Value), i);
            return values;
        }

#if UNITY_EDITOR
        internal static void LogEvent(string name)
        {
        }

        internal static void LogEvent(string name, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
        }
#else
        private const string AnalyticsType = "Firebase.Analytics.FirebaseAnalytics, Firebase.Analytics";
        private static bool searched;
        private static Type parameterType;
        private static MethodInfo logName;
        private static MethodInfo logParameters;

        internal static void LogEvent(string name)
        {
            Find();
            Invoke(logName, name, new object[] { name });
        }

        internal static void LogEvent(string name, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            Find();
            if (logParameters == null)
                return;

            Array values;
            try
            {
                values = Parameters(parameterType, parameters);
            }
            catch (Exception exception)
            {
                HDCOnboardingLog.Warn("Firebase Analytics did not log " + name + ": " + Cause(exception).Message);
                return;
            }

            Invoke(logParameters, name, new object[] { name, values });
        }

        private static void Find()
        {
            if (searched)
                return;
            searched = true;
            Type analytics = Type.GetType(AnalyticsType);
            if (analytics == null)
            {
                HDCOnboardingLog.Log("Firebase Analytics is not in this build, so no onboarding event is sent.");
                return;
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            logName = analytics.GetMethod("LogEvent", flags, null, new[] { typeof(string) }, null);
            parameterType = Type.GetType(ParameterType);
            if (parameterType != null)
                logParameters = analytics.GetMethod("LogEvent", flags, null, new[] { typeof(string), parameterType.MakeArrayType() }, null);
        }

        private static void Invoke(MethodInfo method, string name, object[] arguments)
        {
            if (method == null)
                return;
            try
            {
                method.Invoke(null, arguments);
            }
            catch (Exception exception)
            {
                HDCOnboardingLog.Warn("Firebase Analytics did not log " + name + ": " + Cause(exception).Message);
            }
        }

        private static Exception Cause(Exception exception) =>
            exception is TargetInvocationException && exception.InnerException != null ? exception.InnerException : exception;
#endif
    }
}
