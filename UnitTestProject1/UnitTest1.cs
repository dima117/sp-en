using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SpaceEngineers;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        Vector3D forward = new Vector3D(1, 0, 0);
        Vector3D up = new Vector3D(0, 1, 0);

        [TestMethod]
        public void TestMethod1()
        {
            var orientation = MatrixD.CreateWorld(Vector3D.Zero, forward, up);

            var result = DirectionController.GetNavAngle(orientation, new Vector3D(10, 10, 0));

            Assert.IsTrue(result.Pitch > 0);
            Assert.AreEqual(result.Yaw, 0);
            Assert.AreEqual(result.Roll, 0);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var orientation = MatrixD.CreateWorld(Vector3D.Zero, forward, up);

            var result = DirectionController.GetNavAngle(orientation, new Vector3D(-10, 10, 0));

            Assert.IsTrue(result.Pitch > 0);
            Assert.AreEqual(result.Yaw, 0);
            Assert.AreEqual(result.Roll, 0);
        }
    }
}
