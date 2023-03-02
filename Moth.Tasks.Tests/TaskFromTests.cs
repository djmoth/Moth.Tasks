namespace Moth.Tasks.Tests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public unsafe class TaskFromTests
    {
        [Test]
        public void TestFromAction ()
        {
            var task = Task.From (static () => { throw new Exception (); });
            Assert.Throws<Exception> (task.Run);
        }

        [Test]
        public void TestFromActionWithArg ()
        {
            const int i = 42;

            var task = Task.From (static (int a) => { throw new Exception (a.ToString ()); }, i);

            string message = Assert.Throws<Exception> (task.Run).Message;

            Assert.AreEqual (i.ToString (), message);
        }

        [Test]
        public void TestFromFunctionPointer ()
        {
            var task = Task.From (&ThrowException);
            Assert.Throws<Exception> (task.Run);

            static void ThrowException () => throw new Exception ();
        }

        [Test]
        public void TestFromFunctionPointerWithArg ()
        {
            const int i = 42;

            var task = Task.From (&ThrowException, i);

            string message = Assert.Throws<Exception> (task.Run).Message;

            Assert.AreEqual (i.ToString (), message);

            static void ThrowException (int i) => throw new Exception (i.ToString ());
        }
    }
}
