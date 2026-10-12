using System.Collections.Generic;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCMrecAds : IMrecAds, IAdChannel
    {
        private readonly HDCAdsContext context;
        private HDCRectGroup group;
        private MoveMode moveMode;
        private HDCAdPosition movePosition;
        private Vector2 movePoint;
        private GameObject moveTarget;
        private Camera moveCamera;

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
            return Group().IsLoaded;
        }

        public void Hide() => group?.Hide();

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

        private void ApplyMove()
        {
            if (group == null)
                return;
            for (int i = 0; i < group.StartedCount; i++)
            {
                HDCRectSource source = group.Sources[i];
                switch (moveMode)
                {
                    case MoveMode.Position:
                        source.Move(movePosition);
                        break;
                    case MoveMode.Point:
                        source.Move(movePoint);
                        break;
                    case MoveMode.Target when moveTarget != null:
                        source.Move(TargetPoint());
                        break;
                }
            }
        }

        private Vector2 TargetPoint()
        {
            Vector3 position = moveTarget.transform.position;
            return moveCamera != null ? (Vector2)moveCamera.WorldToScreenPoint(position) : RectTransformUtility.WorldToScreenPoint(null, position);
        }

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
    }
}
