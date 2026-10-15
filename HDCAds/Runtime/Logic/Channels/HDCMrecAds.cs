using System.Collections.Generic;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCMrecAds : IMrecAds, IAdChannel
    {
        private const float StillPixels = 0.5f;
        private static readonly Vector3[] Corners = new Vector3[4];

        private readonly HDCAdsContext context;
        private HDCRectGroup group;
        private MoveMode moveMode;
        private HDCAdPosition movePosition;
        private Vector2 movePoint;
        private GameObject moveTarget;
        private Camera moveCamera;
        private bool following;
        private Pushed pushed;

        internal HDCMrecAds(HDCAdsContext context)
        {
            this.context = context;
        }

        private enum MoveMode
        {
            None,
            Position,
            Point,
            Target,
        }

        private HDCAdsConfig.MrecChannel Channel => context.Config.mrecChannel ?? new HDCAdsConfig.MrecChannel();

        private bool IsEnabled => Channel.isEnabled && !context.IsAdsRemoved;

        public bool CanShow => IsEnabled && Group().IsLoaded;

        public Vector2 SizeInPixels => group != null && group.Sources.Count > 0 ? group.Sources[0].SizeInPixels : Vector2.zero;

        public bool Show()
        {
            if (!IsEnabled || Group().IsEmpty)
                return false;
            Group().Initialize();
            ApplyMove();
            Group().Show();
            Follow(true);
            return Group().IsLoaded;
        }

        public void Hide()
        {
            Follow(false);
            group?.Hide();
        }

        public void Move(HDCAdPosition position)
        {
            if (!IsEnabled)
                return;
            moveMode = MoveMode.Position;
            movePosition = position;
            ApplyMove();
        }

        public void Move(Vector2 screenPoint)
        {
            if (!IsEnabled)
                return;
            moveMode = MoveMode.Point;
            movePoint = screenPoint;
            ApplyMove();
        }

        public void Move(GameObject target, Camera camera = null)
        {
            if (target == null || !IsEnabled)
                return;
            moveMode = MoveMode.Target;
            moveTarget = target;
            moveCamera = camera;
            ApplyMove();
        }

        public void Initialize()
        {
            if (IsEnabled && !Channel.autoInit)
                Group().Initialize();
        }

        string IAdChannel.Key => "MREC";

        string IAdChannel.Title => "Mrec";

        void IAdChannel.OnSdkInitialized()
        {
            if (IsEnabled && Channel.autoInit)
                Group().Initialize();
        }

        void IAdChannel.InitializeAll() => Initialize();

        void IAdChannel.OnAdsRemoved() => Hide();

        internal static Vector2 ScreenCenter(GameObject target, Camera camera)
        {
            Transform transform = target.transform;
            Camera view = CameraFor(transform, camera);
            if (transform is RectTransform rect)
            {
                rect.GetWorldCorners(Corners);
                Vector2 min = RectTransformUtility.WorldToScreenPoint(view, Corners[0]);
                Vector2 max = RectTransformUtility.WorldToScreenPoint(view, Corners[2]);
                return (min + max) / 2f;
            }

            return view != null ? (Vector2)view.WorldToScreenPoint(transform.position) : (Vector2)transform.position;
        }

        internal static Camera CameraFor(Transform target, Camera camera)
        {
            if (camera != null)
                return camera;

            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null)
                return null;

            Canvas root = canvas.rootCanvas;
            switch (root.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    return null;
                case RenderMode.ScreenSpaceCamera:
                    return root.worldCamera;
                default:
                    return root.worldCamera != null ? root.worldCamera : Camera.main;
            }
        }

        private void Follow(bool on)
        {
            if (on == following)
                return;
            following = on;
            if (on)
                context.MainThread.LateTicked += FollowMove;
            else
                context.MainThread.LateTicked -= FollowMove;
        }

        private void ApplyMove() => Push(true);

        private void FollowMove() => Push(false);

        private void Push(bool always)
        {
            if (group == null || moveMode == MoveMode.None || (moveMode == MoveMode.Target && moveTarget == null))
                return;

            var next = new Pushed(moveMode, movePosition, PointToPush(), group.StartedCount, new Vector2Int(Screen.width, Screen.height), Screen.safeArea);
            if (!always && next.Matches(pushed))
                return;
            pushed = next;

            for (int i = 0; i < group.StartedCount; i++)
            {
                HDCRectSource source = group.Sources[i];
                if (moveMode == MoveMode.Position)
                    source.Move(movePosition);
                else
                    source.Move(next.Point);
            }
        }

        private Vector2 PointToPush()
        {
            switch (moveMode)
            {
                case MoveMode.Point:
                    return movePoint;
                case MoveMode.Target:
                    return TargetPoint();
                default:
                    return Vector2.zero;
            }
        }

        private Vector2 TargetPoint() => ScreenCenter(moveTarget, moveCamera);

        private HDCRectGroup Group()
        {
            if (group != null)
                return group;

            List<HDCRectSource> sources = HDCAdGroups.ViewSources(context.Groups.MrecPlans());
            foreach (HDCRectSource source in sources)
                context.Placements.Record(source.Id, HDCAdChannel.Mrec, "", source.Network.RevenueNetwork);
            group = new HDCRectGroup(sources, false, context.Log);
            group.Loaded += ApplyMove;
            return group;
        }

        private readonly struct Pushed
        {
            internal Pushed(MoveMode mode, HDCAdPosition position, Vector2 point, int sources, Vector2Int screenSize, Rect safeArea)
            {
                Mode = mode;
                Position = position;
                Point = point;
                Sources = sources;
                ScreenSize = screenSize;
                SafeArea = safeArea;
            }

            internal MoveMode Mode { get; }
            internal HDCAdPosition Position { get; }
            internal Vector2 Point { get; }
            internal int Sources { get; }
            internal Vector2Int ScreenSize { get; }
            internal Rect SafeArea { get; }

            internal bool Matches(Pushed other) =>
                Mode == other.Mode && Sources == other.Sources && ScreenSize == other.ScreenSize && SafeArea == other.SafeArea &&
                (Mode == MoveMode.Position ? Position == other.Position : (Point - other.Point).sqrMagnitude < StillPixels * StillPixels);
        }
    }
}
