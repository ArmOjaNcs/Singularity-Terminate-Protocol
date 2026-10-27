using ECS.CommonComponents;
using ECS.ProjectileComponents;
using UnityEngine;

namespace Gameplay.Spatial
{
    public static class ColliderGeometry
    {
        private const float Epsilon = 0.000001f;

        public static float DistanceToCollider(
            Vector3 point,
            Vector3 colliderPosition,
            ColliderComponent collider)
        {
            Vector2 target = ToVector2(point);
            Vector2 center = ToVector2(
                GetColliderWorldCenter(colliderPosition, collider));

            switch (collider.Type)
            {
                case ColliderType.Circle:
                    return Mathf.Max(
                        0f,
                        Vector2.Distance(target, center) -
                        Mathf.Max(0f, collider.Radius));

                case ColliderType.Box:
                    return DistanceToBox(target, center, collider);

                case ColliderType.Capsule:
                    return DistanceToCapsule(target, center, collider);

                case ColliderType.Sector:
                    return DistanceToSector(target, center, collider);

                default:
                    return float.MaxValue;
            }
        }

        public static bool Overlaps(
            Vector3 firstPosition,
            ColliderComponent firstCollider,
            Vector3 secondPosition,
            ColliderComponent secondCollider)
        {
            Vector2 firstCenter = ToVector2(
                GetColliderWorldCenter(firstPosition, firstCollider));
            Vector2 secondCenter = ToVector2(
                GetColliderWorldCenter(secondPosition, secondCollider));

            switch (firstCollider.Type)
            {
                case ColliderType.Circle:
                    return CircleOverlaps(
                        firstCenter,
                        firstCollider,
                        secondCenter,
                        secondCollider);

                case ColliderType.Box:
                    return BoxOverlaps(
                        firstCenter,
                        firstCollider,
                        secondCenter,
                        secondCollider);

                case ColliderType.Capsule:
                    return CapsuleOverlaps(
                        firstCenter,
                        firstCollider,
                        secondCenter,
                        secondCollider);

                case ColliderType.Sector:
                    return SectorOverlaps(
                        firstCenter,
                        firstCollider,
                        secondCenter,
                        secondCollider);

                default:
                    return false;
            }
        }

        public static bool OverlapsCircle(
            Vector3 circlePosition,
            float circleRadius,
            Vector3 targetPosition,
            ColliderComponent targetCollider)
        {
            Vector2 center = ToVector2(circlePosition);
            Vector2 target = ToVector2(
                GetColliderWorldCenter(targetPosition, targetCollider));

            return CircleOverlaps(
                center,
                new ColliderComponent
                {
                    Type = ColliderType.Circle,
                    Radius = Mathf.Max(0f, circleRadius)
                },
                target,
                targetCollider);
        }

        public static void GetCapsuleSegment(
            Vector2 center,
            ColliderComponent collider,
            out Vector2 start,
            out Vector2 end)
        {
            float radius = Mathf.Max(0f, collider.Radius);
            float halfLength = Mathf.Max(
                0f,
                Mathf.Max(0f, collider.Length) * 0.5f - radius);

            Vector2 axis = GetDirection(
                (collider.CapsuleOrientation == CapsuleOrientation.Y ? 90f : 0f) +
                collider.Angle);

            start = center - axis * halfLength;
            end = center + axis * halfLength;
        }

        private static bool CircleOverlaps(
            Vector2 center,
            ColliderComponent circle,
            Vector2 otherCenter,
            ColliderComponent other)
        {
            float radius = Mathf.Max(0f, circle.Radius);

            switch (other.Type)
            {
                case ColliderType.Circle:
                    radius += Mathf.Max(0f, other.Radius);
                    return (center - otherCenter).sqrMagnitude <=
                           radius * radius + Epsilon;

                case ColliderType.Box:
                    return DistanceToBox(center, otherCenter, other) <= radius + Epsilon;

                case ColliderType.Capsule:
                    GetCapsuleSegment(
                        otherCenter, other,
                        out Vector2 start, out Vector2 end);

                    radius += Mathf.Max(0f, other.Radius);
                    return DistanceToSegment(center, start, end) <= radius + Epsilon;

                case ColliderType.Sector:
                    return DistanceToSector(center, otherCenter, other) <= radius + Epsilon;

                default:
                    return false;
            }
        }

        public static Vector3 GetColliderWorldCenter(
            Vector3 objectPosition,
            ColliderComponent collider)
        {
            Vector2 localCenter = collider.Center;
            float radians = collider.Angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            Vector2 rotatedCenter = new Vector2(
                localCenter.x * cos - localCenter.y * sin,
                localCenter.x * sin + localCenter.y * cos);

            return objectPosition + (Vector3)rotatedCenter;
        }

        private static bool BoxOverlaps(
            Vector2 center,
            ColliderComponent box,
            Vector2 otherCenter,
            ColliderComponent other)
        {
            switch (other.Type)
            {
                case ColliderType.Circle:
                    return CircleOverlaps(otherCenter, other, center, box);

                case ColliderType.Box:
                    return BoxBoxOverlap(center, box, otherCenter, other);

                case ColliderType.Capsule:
                    return BoxCapsuleOverlap(center, box, otherCenter, other);

                case ColliderType.Sector:
                    return BoxSectorOverlap(center, box, otherCenter, other);

                default:
                    return false;
            }
        }

        private static bool CapsuleOverlaps(
            Vector2 center,
            ColliderComponent capsule,
            Vector2 otherCenter,
            ColliderComponent other)
        {
            switch (other.Type)
            {
                case ColliderType.Circle:
                    return CircleOverlaps(otherCenter, other, center, capsule);

                case ColliderType.Box:
                    return BoxCapsuleOverlap(otherCenter, other, center, capsule);

                case ColliderType.Capsule:
                    GetCapsuleSegment(center, capsule, out Vector2 a, out Vector2 b);
                    GetCapsuleSegment(otherCenter, other, out Vector2 c, out Vector2 d);

                    float radius = Mathf.Max(0f, capsule.Radius) +
                                   Mathf.Max(0f, other.Radius);

                    return SegmentDistanceSquared(a, b, c, d) <=
                           radius * radius + Epsilon;

                case ColliderType.Sector:
                    return SectorCapsuleOverlap(otherCenter, other, center, capsule);

                default:
                    return false;
            }
        }

        private static bool SectorOverlaps(
            Vector2 center,
            ColliderComponent sector,
            Vector2 otherCenter,
            ColliderComponent other)
        {
            switch (other.Type)
            {
                case ColliderType.Circle:
                    return CircleOverlaps(otherCenter, other, center, sector);

                case ColliderType.Box:
                    return BoxSectorOverlap(otherCenter, other, center, sector);

                case ColliderType.Capsule:
                    return SectorCapsuleOverlap(center, sector, otherCenter, other);

                case ColliderType.Sector:
                    return SectorSectorOverlap(center, sector, otherCenter, other);

                default:
                    return false;
            }
        }

        private static bool BoxBoxOverlap(
            Vector2 firstCenter,
            ColliderComponent first,
            Vector2 secondCenter,
            ColliderComponent second)
        {
            GetBoxAxes(first.Angle, out Vector2 ax, out Vector2 ay);
            GetBoxAxes(second.Angle, out Vector2 bx, out Vector2 by);

            Vector2 offset = secondCenter - firstCenter;

            return !Separated(offset, ax, first, second, ax, ay, bx, by) &&
                   !Separated(offset, ay, first, second, ax, ay, bx, by) &&
                   !Separated(offset, bx, first, second, ax, ay, bx, by) &&
                   !Separated(offset, by, first, second, ax, ay, bx, by);
        }

        private static bool Separated(
            Vector2 offset,
            Vector2 axis,
            ColliderComponent first,
            ColliderComponent second,
            Vector2 ax,
            Vector2 ay,
            Vector2 bx,
            Vector2 by)
        {
            float firstRadius =
                Mathf.Max(0f, first.Size.x) * 0.5f * Mathf.Abs(Vector2.Dot(ax, axis)) +
                Mathf.Max(0f, first.Size.y) * 0.5f * Mathf.Abs(Vector2.Dot(ay, axis));

            float secondRadius =
                Mathf.Max(0f, second.Size.x) * 0.5f * Mathf.Abs(Vector2.Dot(bx, axis)) +
                Mathf.Max(0f, second.Size.y) * 0.5f * Mathf.Abs(Vector2.Dot(by, axis));

            return Mathf.Abs(Vector2.Dot(offset, axis)) >
                   firstRadius + secondRadius + Epsilon;
        }

        private static bool BoxCapsuleOverlap(
            Vector2 boxCenter,
            ColliderComponent box,
            Vector2 capsuleCenter,
            ColliderComponent capsule)
        {
            GetCapsuleSegment(
                capsuleCenter, capsule,
                out Vector2 start, out Vector2 end);

            if (PointInBox(start, boxCenter, box) ||
                PointInBox(end, boxCenter, box))
                return true;

            GetBoxCorners(boxCenter, box,
                out Vector2 a, out Vector2 b,
                out Vector2 c, out Vector2 d);

            float radius = Mathf.Max(0f, capsule.Radius);
            float radiusSquared = radius * radius;

            return SegmentDistanceSquared(start, end, a, b) <= radiusSquared + Epsilon ||
                   SegmentDistanceSquared(start, end, b, c) <= radiusSquared + Epsilon ||
                   SegmentDistanceSquared(start, end, c, d) <= radiusSquared + Epsilon ||
                   SegmentDistanceSquared(start, end, d, a) <= radiusSquared + Epsilon;
        }

        private static bool BoxSectorOverlap(
            Vector2 boxCenter,
            ColliderComponent box,
            Vector2 sectorCenter,
            ColliderComponent sector)
        {
            if (PointInBox(sectorCenter, boxCenter, box) ||
                PointInSector(boxCenter, sectorCenter, sector))
                return true;

            GetBoxCorners(boxCenter, box,
                out Vector2 a, out Vector2 b,
                out Vector2 c, out Vector2 d);

            if (PointInSector(a, sectorCenter, sector) ||
                PointInSector(b, sectorCenter, sector) ||
                PointInSector(c, sectorCenter, sector) ||
                PointInSector(d, sectorCenter, sector))
                return true;

            GetSectorEdges(sectorCenter, sector,
                out Vector2 left, out Vector2 right);

            if (SegmentIntersectsBox(sectorCenter, left, boxCenter, box) ||
                SegmentIntersectsBox(sectorCenter, right, boxCenter, box))
                return true;

            float radius = Mathf.Max(0f, sector.Radius);
            float halfAngle = GetHalfAngle(sector);

            return ArcIntersectsSegment(
                       sectorCenter, radius, sector.Angle, halfAngle, a, b) ||
                   ArcIntersectsSegment(
                       sectorCenter, radius, sector.Angle, halfAngle, b, c) ||
                   ArcIntersectsSegment(
                       sectorCenter, radius, sector.Angle, halfAngle, c, d) ||
                   ArcIntersectsSegment(
                       sectorCenter, radius, sector.Angle, halfAngle, d, a);
        }

        private static bool SectorCapsuleOverlap(
            Vector2 sectorCenter,
            ColliderComponent sector,
            Vector2 capsuleCenter,
            ColliderComponent capsule)
        {
            GetCapsuleSegment(
                capsuleCenter, capsule,
                out Vector2 start, out Vector2 end);

            if (PointInSector(start, sectorCenter, sector) ||
                PointInSector(end, sectorCenter, sector) ||
                SegmentIntersectsSector(start, end, sectorCenter, sector))
                return true;

            float radius = Mathf.Max(0f, capsule.Radius);

            if (DistanceToSector(start, sectorCenter, sector) <= radius + Epsilon ||
                DistanceToSector(end, sectorCenter, sector) <= radius + Epsilon)
                return true;

            GetSectorEdges(sectorCenter, sector,
                out Vector2 left, out Vector2 right);

            float distanceSquared = Mathf.Min(
                SegmentDistanceSquared(start, end, sectorCenter, left),
                SegmentDistanceSquared(start, end, sectorCenter, right));

            distanceSquared = Mathf.Min(
                distanceSquared,
                SegmentArcDistanceSquared(
                    start, end, sectorCenter,
                    Mathf.Max(0f, sector.Radius),
                    sector.Angle, GetHalfAngle(sector)));

            return distanceSquared <= radius * radius + Epsilon;
        }

        private static bool SectorSectorOverlap(
            Vector2 firstCenter,
            ColliderComponent first,
            Vector2 secondCenter,
            ColliderComponent second)
        {
            if (PointInSector(firstCenter, secondCenter, second) ||
                PointInSector(secondCenter, firstCenter, first))
                return true;

            GetSectorEdges(firstCenter, first,
                out Vector2 firstLeft, out Vector2 firstRight);

            GetSectorEdges(secondCenter, second,
                out Vector2 secondLeft, out Vector2 secondRight);

            if (PointInSector(firstLeft, secondCenter, second) ||
                PointInSector(firstRight, secondCenter, second) ||
                PointInSector(secondLeft, firstCenter, first) ||
                PointInSector(secondRight, firstCenter, first))
                return true;

            if (SegmentIntersectsSector(firstCenter, firstLeft, secondCenter, second) ||
                SegmentIntersectsSector(firstCenter, firstRight, secondCenter, second) ||
                SegmentIntersectsSector(secondCenter, secondLeft, firstCenter, first) ||
                SegmentIntersectsSector(secondCenter, secondRight, firstCenter, first))
                return true;

            return CircleArcsIntersect(
                firstCenter, first.Radius, first.Angle, GetHalfAngle(first),
                secondCenter, second.Radius, second.Angle, GetHalfAngle(second));
        }

        private static bool SegmentIntersectsSector(
            Vector2 start,
            Vector2 end,
            Vector2 center,
            ColliderComponent sector)
        {
            if (PointInSector(start, center, sector) ||
                PointInSector(end, center, sector))
                return true;

            GetSectorEdges(center, sector, out Vector2 left, out Vector2 right);

            return SegmentsIntersect(start, end, center, left) ||
                   SegmentsIntersect(start, end, center, right) ||
                   ArcIntersectsSegment(
                       center, Mathf.Max(0f, sector.Radius),
                       sector.Angle, GetHalfAngle(sector), start, end);
        }

        private static bool SegmentIntersectsBox(
            Vector2 start,
            Vector2 end,
            Vector2 center,
            ColliderComponent box)
        {
            if (PointInBox(start, center, box) || PointInBox(end, center, box))
                return true;

            GetBoxCorners(center, box,
                out Vector2 a, out Vector2 b,
                out Vector2 c, out Vector2 d);

            return SegmentsIntersect(start, end, a, b) ||
                   SegmentsIntersect(start, end, b, c) ||
                   SegmentsIntersect(start, end, c, d) ||
                   SegmentsIntersect(start, end, d, a);
        }

        private static bool CircleArcsIntersect(
            Vector2 firstCenter,
            float firstRadius,
            float firstAngle,
            float firstHalfAngle,
            Vector2 secondCenter,
            float secondRadius,
            float secondAngle,
            float secondHalfAngle)
        {
            firstRadius = Mathf.Max(0f, firstRadius);
            secondRadius = Mathf.Max(0f, secondRadius);

            Vector2 delta = secondCenter - firstCenter;
            float distance = delta.magnitude;

            if (distance <= Epsilon ||
                distance > firstRadius + secondRadius + Epsilon ||
                distance < Mathf.Abs(firstRadius - secondRadius) - Epsilon)
                return false;

            float along = (firstRadius * firstRadius -
                           secondRadius * secondRadius +
                           distance * distance) / (2f * distance);

            float heightSquared = firstRadius * firstRadius - along * along;

            if (heightSquared < -Epsilon)
                return false;

            float height = Mathf.Sqrt(Mathf.Max(0f, heightSquared));
            Vector2 direction = delta / distance;
            Vector2 basePoint = firstCenter + direction * along;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);

            Vector2 pointA = basePoint + perpendicular * height;
            Vector2 pointB = basePoint - perpendicular * height;

            return PointOnArc(pointA, firstCenter, firstAngle, firstHalfAngle) &&
                   PointOnArc(pointA, secondCenter, secondAngle, secondHalfAngle) ||
                   PointOnArc(pointB, firstCenter, firstAngle, firstHalfAngle) &&
                   PointOnArc(pointB, secondCenter, secondAngle, secondHalfAngle);
        }

        private static bool ArcIntersectsSegment(
            Vector2 center,
            float radius,
            float angle,
            float halfAngle,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared <= Epsilon)
            {
                return Mathf.Abs(Vector2.Distance(start, center) - radius) <= Epsilon &&
                       PointOnArc(start, center, angle, halfAngle);
            }

            Vector2 offset = start - center;
            float b = 2f * Vector2.Dot(offset, segment);
            float c = offset.sqrMagnitude - radius * radius;
            float discriminant = b * b - 4f * lengthSquared * c;

            if (discriminant < -Epsilon)
                return false;

            float root = Mathf.Sqrt(Mathf.Max(0f, discriminant));
            float inverse = 0.5f / lengthSquared;
            float t1 = (-b - root) * inverse;
            float t2 = (-b + root) * inverse;

            if (t1 >= -Epsilon && t1 <= 1f + Epsilon &&
                PointOnArc(
                    start + segment * Mathf.Clamp01(t1),
                    center, angle, halfAngle))
                return true;

            return t2 >= -Epsilon && t2 <= 1f + Epsilon &&
                   PointOnArc(
                       start + segment * Mathf.Clamp01(t2),
                       center, angle, halfAngle);
        }

        private static float SegmentArcDistanceSquared(
            Vector2 start,
            Vector2 end,
            Vector2 center,
            float radius,
            float angle,
            float halfAngle)
        {
            float best = Mathf.Min(
                DistanceToSector(start, center, CreateSector(radius, angle, halfAngle)),
                DistanceToSector(end, center, CreateSector(radius, angle, halfAngle)));

            best *= best;

            Vector2 axis = GetDirection(angle);
            Vector2 radial = center + axis * radius;
            float distance = DistanceToSegment(radial, start, end);
            best = Mathf.Min(best, distance * distance);

            Vector2 left = center + Rotate(axis, halfAngle) * radius;
            Vector2 right = center + Rotate(axis, -halfAngle) * radius;

            distance = DistanceToSegment(left, start, end);
            best = Mathf.Min(best, distance * distance);

            distance = DistanceToSegment(right, start, end);
            best = Mathf.Min(best, distance * distance);

            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared > Epsilon)
            {
                float t = Mathf.Clamp01(
                    Vector2.Dot(center - start, segment) / lengthSquared);

                Vector2 closest = start + segment * t;
                Vector2 offset = closest - center;

                if (offset.sqrMagnitude > Epsilon)
                {
                    Vector2 arcPoint = center + offset.normalized * radius;

                    if (PointOnArc(arcPoint, center, angle, halfAngle))
                    {
                        distance = DistanceToSegment(arcPoint, start, end);
                        best = Mathf.Min(best, distance * distance);
                    }
                }
            }

            return best;
        }

        private static ColliderComponent CreateSector(
            float radius,
            float angle,
            float halfAngle)
        {
            return new ColliderComponent
            {
                Type = ColliderType.Sector,
                Radius = radius,
                Angle = angle,
                Size = new Vector2(halfAngle * 2f, 0f)
            };
        }

        private static void GetSectorEdges(
            Vector2 center,
            ColliderComponent sector,
            out Vector2 left,
            out Vector2 right)
        {
            Vector2 axis = GetDirection(sector.Angle);
            float radius = Mathf.Max(0f, sector.Radius);
            float halfAngle = GetHalfAngle(sector);

            left = center + Rotate(axis, halfAngle) * radius;
            right = center + Rotate(axis, -halfAngle) * radius;
        }

        private static bool PointInSector(
            Vector2 point,
            Vector2 center,
            ColliderComponent sector)
        {
            Vector2 offset = point - center;
            float radius = Mathf.Max(0f, sector.Radius);

            if (offset.sqrMagnitude > radius * radius + Epsilon)
                return false;

            if (offset.sqrMagnitude <= Epsilon)
                return true;

            return Mathf.Abs(Vector2.SignedAngle(
                       GetDirection(sector.Angle), offset)) <=
                   GetHalfAngle(sector) + Epsilon;
        }

        private static bool PointOnArc(
            Vector2 point,
            Vector2 center,
            float angle,
            float halfAngle)
        {
            Vector2 offset = point - center;

            return offset.sqrMagnitude > Epsilon &&
                   Mathf.Abs(Vector2.SignedAngle(
                       GetDirection(angle), offset)) <= halfAngle + Epsilon;
        }

        private static float DistanceToBox(
            Vector2 point,
            Vector2 center,
            ColliderComponent box)
        {
            Vector2 local = Rotate(point - center, -box.Angle);
            Vector2 halfSize = new Vector2(
                Mathf.Max(0f, box.Size.x) * 0.5f,
                Mathf.Max(0f, box.Size.y) * 0.5f);

            Vector2 closest = new Vector2(
                Mathf.Clamp(local.x, -halfSize.x, halfSize.x),
                Mathf.Clamp(local.y, -halfSize.y, halfSize.y));

            return Vector2.Distance(local, closest);
        }

        private static float DistanceToCapsule(
            Vector2 point,
            Vector2 center,
            ColliderComponent capsule)
        {
            GetCapsuleSegment(center, capsule, out Vector2 start, out Vector2 end);

            return Mathf.Max(
                0f,
                DistanceToSegment(point, start, end) -
                Mathf.Max(0f, capsule.Radius));
        }

        private static float DistanceToSector(
            Vector2 point,
            Vector2 center,
            ColliderComponent sector)
        {
            if (PointInSector(point, center, sector))
                return 0f;

            GetSectorEdges(center, sector, out Vector2 left, out Vector2 right);

            float best = Mathf.Min(
                DistanceToSegment(point, center, left),
                DistanceToSegment(point, center, right));

            Vector2 offset = point - center;

            if (offset.sqrMagnitude > Epsilon)
            {
                Vector2 arcPoint = center +
                    offset.normalized * Mathf.Max(0f, sector.Radius);

                if (PointOnArc(
                    arcPoint, center, sector.Angle, GetHalfAngle(sector)))
                {
                    best = Mathf.Min(
                        best,
                        Mathf.Abs(offset.magnitude -
                                  Mathf.Max(0f, sector.Radius)));
                }
            }

            return best;
        }

        private static bool PointInBox(
            Vector2 point,
            Vector2 center,
            ColliderComponent box)
        {
            Vector2 local = Rotate(point - center, -box.Angle);

            return Mathf.Abs(local.x) <=
                       Mathf.Max(0f, box.Size.x) * 0.5f + Epsilon &&
                   Mathf.Abs(local.y) <=
                       Mathf.Max(0f, box.Size.y) * 0.5f + Epsilon;
        }

        private static void GetBoxCorners(
            Vector2 center,
            ColliderComponent box,
            out Vector2 a,
            out Vector2 b,
            out Vector2 c,
            out Vector2 d)
        {
            float x = Mathf.Max(0f, box.Size.x) * 0.5f;
            float y = Mathf.Max(0f, box.Size.y) * 0.5f;

            a = center + Rotate(new Vector2(-x, -y), box.Angle);
            b = center + Rotate(new Vector2(x, -y), box.Angle);
            c = center + Rotate(new Vector2(x, y), box.Angle);
            d = center + Rotate(new Vector2(-x, y), box.Angle);
        }

        private static void GetBoxAxes(
            float angle,
            out Vector2 axisX,
            out Vector2 axisY)
        {
            axisX = GetDirection(angle);
            axisY = new Vector2(-axisX.y, axisX.x);
        }

        private static float DistanceToSegment(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared <= Epsilon)
                return Vector2.Distance(point, start);

            float t = Mathf.Clamp01(
                Vector2.Dot(point - start, segment) / lengthSquared);

            return Vector2.Distance(point, start + segment * t);
        }

        private static float SegmentDistanceSquared(
            Vector2 p1,
            Vector2 q1,
            Vector2 p2,
            Vector2 q2)
        {
            Vector2 d1 = q1 - p1;
            Vector2 d2 = q2 - p2;
            Vector2 r = p1 - p2;

            float a = Vector2.Dot(d1, d1);
            float e = Vector2.Dot(d2, d2);
            float f = Vector2.Dot(d2, r);
            float s;
            float t;

            if (a <= Epsilon && e <= Epsilon)
                return (p1 - p2).sqrMagnitude;

            if (a <= Epsilon)
            {
                s = 0f;
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                float c = Vector2.Dot(d1, r);

                if (e <= Epsilon)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector2.Dot(d1, d2);
                    float denominator = a * e - b * b;

                    s = denominator != 0f
                        ? Mathf.Clamp01((b * f - c * e) / denominator)
                        : 0f;

                    t = (b * s + f) / e;

                    if (t < 0f)
                    {
                        t = 0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }

            Vector2 first = p1 + d1 * s;
            Vector2 second = p2 + d2 * t;

            return (first - second).sqrMagnitude;
        }

        private static bool SegmentsIntersect(
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d)
        {
            Vector2 ab = b - a;
            Vector2 cd = d - c;
            float denominator = Cross(ab, cd);
            Vector2 offset = c - a;

            if (Mathf.Abs(denominator) <= Epsilon)
            {
                if (Mathf.Abs(Cross(offset, ab)) > Epsilon)
                    return false;

                float lengthSquared = ab.sqrMagnitude;

                if (lengthSquared <= Epsilon)
                    return (a - c).sqrMagnitude <= Epsilon ||
                           (a - d).sqrMagnitude <= Epsilon;

                float t0 = Vector2.Dot(c - a, ab) / lengthSquared;
                float t1 = Vector2.Dot(d - a, ab) / lengthSquared;

                return Mathf.Max(Mathf.Min(t0, t1), 0f) <=
                       Mathf.Min(Mathf.Max(t0, t1), 1f) + Epsilon;
            }

            float t = Cross(offset, cd) / denominator;
            float u = Cross(offset, ab) / denominator;

            return t >= -Epsilon && t <= 1f + Epsilon &&
                   u >= -Epsilon && u <= 1f + Epsilon;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private static float GetHalfAngle(ColliderComponent sector)
        {
            return Mathf.Clamp(sector.Size.x * 0.5f, 0f, 90f);
        }

        private static Vector2 GetDirection(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;

            return new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians));
        }

        private static Vector2 Rotate(Vector2 value, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                value.x * cos - value.y * sin,
                value.x * sin + value.y * cos);
        }

        private static Vector2 ToVector2(Vector3 value)
        {
            return new Vector2(value.x, value.y);
        }
    }
}
