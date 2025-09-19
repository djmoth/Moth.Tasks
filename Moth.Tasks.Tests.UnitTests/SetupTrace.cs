namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;
    using System.Diagnostics;

    [TestFixture]
    public class SetupTrace
    {
        [OneTimeSetUp]
        public void Setup ()
        {
            Trace.Listeners.Add (new ConsoleTraceListener ());
        }

        [OneTimeTearDown]
        public void Teardown ()
        {
            Trace.Flush ();
        }
    }
}
