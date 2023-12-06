using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRageMath;

namespace SpaceEngineers
{
    #region Copy

    public static class Serializer
    {
        const char DIV = ';';

        public static void SerializeMatrixD(MatrixD m, StringBuilder sb)
        {
            var a = new double[] {
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44,
            };

            sb.AppendLine(string.Join(DIV.ToString(), a));
        }

        public static string SerializeMyDetectedEntityInfo(MyDetectedEntityInfo entity)
        {
            var sb = new StringBuilder();

            sb.AppendLine(entity.EntityId.ToString()); // 0
            sb.AppendLine(entity.Type.ToString()); // 1
            sb.AppendLine(entity.HitPosition?.ToString() ?? string.Empty); // 2

            SerializeMatrixD(entity.Orientation, sb); // 3

            sb.AppendLine(entity.Velocity.ToString()); // 4
            sb.AppendLine(entity.Relationship.ToString()); // 5
            sb.AppendLine(entity.BoundingBox.Min.ToString()); // 6
            sb.AppendLine(entity.BoundingBox.Max.ToString()); // 7
            sb.AppendLine(entity.TimeStamp.ToString()); // 8

            return sb.ToString();
        }

        public static bool TryParseMatrixD(string str, out MatrixD m)
        {
            try
            {
                var a = str.Split(DIV).Select(s => double.Parse(s)).ToArray();

                m = new MatrixD(
                    a[0], a[1], a[2], a[3],
                    a[4], a[5], a[6], a[7],
                    a[8], a[9], a[10], a[11],
                    a[12], a[13], a[14], a[15]
                );

                return true;
            }
            catch
            {
                m = MatrixD.Zero;
                return false;
            }
        }

        public static bool TryParseNullableVector3D(string str, out Vector3D? v)
        {
            if (string.IsNullOrEmpty(str))
            {
                v = null;

                return true;
            }

            Vector3D tmp;

            if (Vector3D.TryParse(str, out tmp))
            {
                v = tmp;

                return true;
            }

            v = null;

            return false;
        }

        public static bool TryParseBoundingBoxD(string strMin, string strMax, out BoundingBoxD b)
        {
            Vector3D min, max;

            if (Vector3D.TryParse(strMin, out min) && Vector3D.TryParse(strMax, out max))
            {
                b = new BoundingBoxD(min, max);

                return true;
            }

            b = new BoundingBoxD();

            return false;
        }

        public static bool TryParseMyDetectedEntityInfo(string[] lines, out MyDetectedEntityInfo entity)
        {
            var success = false;

            // 0 - entity id
            long entityId;
            success &= long.TryParse(lines[0], out entityId);

            // 1 - type
            MyDetectedEntityType type;
            success &= MyDetectedEntityType.TryParse(lines[1], out type);

            // 2 - hit position
            Vector3D? hitPosition;
            success &= TryParseNullableVector3D(lines[2], out hitPosition);

            // 3 - orientation
            MatrixD orientation;
            success &= TryParseMatrixD(lines[3], out orientation);

            // 4 - velocity
            Vector3D velocity;
            success &= Vector3D.TryParse(lines[4], out velocity);

            // 5 - relationship
            MyRelationsBetweenPlayerAndBlock relationship;
            success &= MyRelationsBetweenPlayerAndBlock.TryParse(lines[5], out relationship);

            // 6, 7 - boundingBox
            BoundingBoxD boundingBox;
            success &= TryParseBoundingBoxD(lines[6], lines[7], out boundingBox);


            // 8 - timestamp
            long timestamp;
            success &= long.TryParse(lines[8], out timestamp);

            entity = success
                ? new MyDetectedEntityInfo(
                    entityId, string.Empty, type, hitPosition, orientation,
                    velocity, relationship, boundingBox, timestamp
                ) : new MyDetectedEntityInfo();


            return success;
        }
    }

    #endregion
}
