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
using SpaceEngineers;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        static Vector3D forward = new Vector3D(1, 0, 0);
        static Vector3D up = new Vector3D(0, 1, 0);
        static MatrixD orientation = MatrixD.CreateWorld(Vector3D.Zero, forward, up);

        static double h30 = 17.320508075688775;

        [TestMethod]
        public void TestMethod_Прямо()
        {
            var result = DirectionController.GetNavAngle(orientation, new Vector3D(10, 0, 0));

            Assert.AreEqual(result.Pitch, 0);
            Assert.AreEqual(result.Yaw, 0);
            Assert.AreEqual(result.Roll, 0);
        }

        [TestMethod]
        public void TestMethod_Назад()
        {
            var result = DirectionController.GetNavAngle(orientation, new Vector3D(-10, 0, 0));

            Assert.AreEqual(result.Pitch, Math.PI, 0.0001);
            Assert.AreEqual(result.Yaw, 0);
            Assert.AreEqual(result.Roll, 0);
        }

        [TestMethod]
        public void TestMethod_30_Наверх()
        {
            var result = DirectionController.GetNavAngle(orientation, new Vector3D(h30, 10, 0));

            Assert.AreEqual(result.Pitch, Math.PI / 6, 0.0001);
            Assert.AreEqual(result.Yaw, 0);
            Assert.AreEqual(result.Roll, 0);
        }

        [TestMethod]
        public void TestMethod_90_Наверх()
        {
            var result = DirectionController.GetNavAngle(orientation, new Vector3D(0, 10, 0));

            Assert.AreEqual(result.Pitch, Math.PI / 2);
            Assert.AreEqual(result.Yaw, 0);
            Assert.AreEqual(result.Roll, 0);
        }

        [TestMethod]
        public void TestMethod_150_Наверх()
        {
            var result = DirectionController.GetNavAngle(orientation, new Vector3D(-h30, 10, 0));

            Assert.AreEqual(result.Pitch, Math.PI * 5 / 6, 0.0001);
            Assert.AreEqual(result.Yaw, 0);
            Assert.AreEqual(result.Roll, 0);
        }

        [TestMethod]
        public void TestMethod_30_Налево()
        {
            var result = DirectionController.GetNavAngle(orientation, new Vector3D(h30, 0, -10));

            Assert.AreEqual(result.Pitch, 0);
            Assert.AreEqual(result.Yaw, -Math.PI / 6, 0.0001);
            Assert.AreEqual(result.Roll, 0);
        }


        [TestMethod]
        public void TestMethod_90_Налево()
        {
            var result = DirectionController.GetNavAngle(orientation, new Vector3D(0, 0, -10));

            Assert.AreEqual(result.Pitch, 0);
            Assert.AreEqual(result.Yaw, -Math.PI / 2);
            Assert.AreEqual(result.Roll, 0);
        }


        [TestMethod]
        public void TestMethod_150_Налево()
        {
            var result = DirectionController.GetNavAngle(orientation, new Vector3D(-h30, 0, -10));

            Assert.AreEqual(result.Pitch, 0);
            Assert.AreEqual(result.Yaw, -Math.PI * 5 / 6, 0.0001);
            Assert.AreEqual(result.Roll, 0);
        }

        [TestMethod]
        public void TestMethod1()
        {
            var target = new Vector3D(-60360.1918753061, -78715.305455653, -61364.0031246584);
            var missile = new Vector3D(-59707.3151551012, -78064.6748712491, -61754.5916689229);
            var missileForward = new Vector3D(0.964023470878601, 0.235865533351898, -0.122581541538239);
            var missileUp = new Vector3D(-0.160490095615387, 0.884069502353668, 0.438935190439224);


            var orientation = MatrixD.CreateWorld(missile, missileForward, missileUp);

            var result = DirectionController.GetNavAngle(orientation, target);

            Assert.AreEqual(result.Pitch, -2.30182503963019, 0.0001);
            Assert.AreEqual(result.Yaw, -0.31063059267177, 0.0001);
            Assert.AreEqual(result.Roll, 0);
        }
    }
}
