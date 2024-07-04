using System.Diagnostics;
using System.Reflection;

using Xunit.Sdk;

namespace BlogPlatform.Api.IntegrationTest
{
    public interface ITestDataReset
    {
        static abstract void ResetData();
    }

    public class ResetDataAfterTestAttribute : BeforeAfterTestAttribute
    {
        public override void After(MethodInfo methodUnderTest)
        {
            Type testDeclaredClass = methodUnderTest.DeclaringType!;
            Type? resetInterfaceType = testDeclaredClass.GetInterface("ITestDataReset")!;
            Debug.Assert(resetInterfaceType is not null);

            MethodInfo resetDataMethodInfo = testDeclaredClass.GetMethod("ResetData", BindingFlags.Public | BindingFlags.Static)!;
            resetDataMethodInfo.Invoke(null, null);
        }
    }
}
