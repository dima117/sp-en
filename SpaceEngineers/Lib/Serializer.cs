using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRageMath;

namespace SpaceEngineers.Lib
{
    #region Copy

    // import:TargetInfo.cs

    public static class Serializer
    {
        const char DIV = ';';

        public class StringReader
        {
            private readonly string[] lines;
            private int nextPos = 0;

            public StringReader(string value)
            {
                lines = value.Split('\n');
            }

            public string GetNextLine()
            {
                return lines[nextPos++];
            }
        }

        public static void SerializeVector3D(Vector3D v, StringBuilder sb)
        {
            sb.AppendLine(v.ToString());
        }

        public static void SerializeDateTime(DateTime d, StringBuilder sb)
        {
            sb.AppendLine(d.ToBinary().ToString());
        }

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

        public static void SerializeMyDetectedEntityInfo(MyDetectedEntityInfo entity, StringBuilder sb)
        {
            sb.AppendLine(entity.EntityId.ToString()); // 0
            sb.AppendLine(entity.Type.ToString()); // 1
            sb.AppendLine(entity.HitPosition?.ToString() ?? string.Empty); // 2

            SerializeMatrixD(entity.Orientation, sb); // 3

            sb.AppendLine(new Vector3D(entity.Velocity).ToString()); // 4
            sb.AppendLine(entity.Relationship.ToString()); // 5
            sb.AppendLine(entity.BoundingBox.Min.ToString()); // 6
            sb.AppendLine(entity.BoundingBox.Max.ToString()); // 7
            sb.AppendLine(entity.TimeStamp.ToString()); // 8
        }

        public static void SerializeTargetInfo(TargetInfo t, StringBuilder sb)
        {
            SerializeMyDetectedEntityInfo(t.Entity, sb);
            SerializeVector3D(t.HitPos, sb);
            SerializeDateTime(t.Timestamp, sb);
        }

        public static bool TryParseMatrixD(StringReader reader, out MatrixD m)
        {
            try
            {
                var a = reader.GetNextLine().Split(DIV).Select(s => double.Parse(s)).ToArray();

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

        public static bool TryParseNullableVector3D(StringReader reader, out Vector3D? v)
        {
            var str = reader.GetNextLine();

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

        public static bool TryParseBoundingBoxD(StringReader reader, out BoundingBoxD b)
        {
            string strMin = reader.GetNextLine();
            string strMax = reader.GetNextLine();

            Vector3D min, max;

            if (Vector3D.TryParse(strMin, out min) && Vector3D.TryParse(strMax, out max))
            {
                b = new BoundingBoxD(min, max);

                return true;
            }

            b = new BoundingBoxD();
            return false;
        }

        public static bool TryParseMyDetectedEntityInfo(
            StringReader reader,
            out MyDetectedEntityInfo entity
        )
        {
            var success = true;

            // 0 - entity id
            long entityId;
            success &= long.TryParse(reader.GetNextLine(), out entityId);

            // 1 - type
            MyDetectedEntityType type;
            success &= MyDetectedEntityType.TryParse(reader.GetNextLine(), out type);

            // 2 - hit position
            Vector3D? hitPosition;
            success &= TryParseNullableVector3D(reader, out hitPosition);

            // 3 - orientation
            MatrixD orientation;
            success &= TryParseMatrixD(reader, out orientation);

            // 4 - velocity
            Vector3D velocity;
            success &= Vector3D.TryParse(reader.GetNextLine(), out velocity);

            // 5 - relationship
            MyRelationsBetweenPlayerAndBlock relationship;
            success &= MyRelationsBetweenPlayerAndBlock.TryParse(reader.GetNextLine(), out relationship);

            // 6, 7 - boundingBox
            BoundingBoxD boundingBox;
            success &= TryParseBoundingBoxD(reader, out boundingBox);

            // 8 - timestamp
            long timestamp;
            success &= long.TryParse(reader.GetNextLine(), out timestamp);

            entity = success
                ? new MyDetectedEntityInfo(
                    entityId, string.Empty, type, hitPosition, orientation,
                    velocity, relationship, boundingBox, timestamp
                ) : new MyDetectedEntityInfo();

            return success;
        }

        public static bool TryParseVector3D(StringReader reader, out Vector3D v)
        {
            return Vector3D.TryParse(reader.GetNextLine(), out v);
        }

        public static bool TryParseDateTime(StringReader reader, out DateTime d)
        {
            long ts;

            if (long.TryParse(reader.GetNextLine(), out ts))
            {
                d = DateTime.FromBinary(ts);
                return true;
            }

            d = DateTime.MinValue;

            return false;
        }

        public static bool TryParseTargetInfo(
            StringReader reader, 
            out TargetInfo targetInfo)
        {
            targetInfo = new TargetInfo();

            MyDetectedEntityInfo entity;
            if (!TryParseMyDetectedEntityInfo(reader, out entity)) return false;

            Vector3D hitPos;
            if (!TryParseVector3D(reader, out hitPos)) return false;

            DateTime timestamp;
            if (!TryParseDateTime(reader, out timestamp)) return false;

            targetInfo = new TargetInfo(entity, timestamp, 0, hitPos);
            return true;
        }
    }

    #endregion
}
