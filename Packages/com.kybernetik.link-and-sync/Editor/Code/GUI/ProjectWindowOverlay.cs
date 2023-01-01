// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LinkAndSync
{
    internal abstract class ProjectWindowOverlay : IEnumerable<LinkAndSync>
    {
        /************************************************************************************************************************/

        private static readonly GUIContent
            ToggleContent = new GUIContent("Project Window Overlay", "This setting is shared by all links"),
            OverlayContent = new GUIContent();
        private static readonly StringBuilder
            TooltipBuilder = new StringBuilder();

        private static readonly Dictionary<string, ProjectWindowOverlay>
            GUIDToOverlay = new Dictionary<string, ProjectWindowOverlay>();
        private static readonly Dictionary<LinkAndSync, SingleLinkOverlay>
            SingleLinkOverlays = new Dictionary<LinkAndSync, SingleLinkOverlay>();

        /************************************************************************************************************************/

        protected abstract bool IsOutOfDate { get; }

        protected abstract void Sync();

        public abstract IEnumerator<LinkAndSync> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.projectWindowItemOnGUI += DrawOverlay;
            AssetDatabaseWatcher.OnPostprocessAssets += delegate { ClearCache(); };
        }

        /************************************************************************************************************************/

        private static GUIStyle _OverlayStyle;

        private static void DrawOverlay(string guid, Rect area)
        {
            if (!LasSettings.EnableOverlay ||
                string.IsNullOrEmpty(guid))
                return;

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                case EventType.MouseUp:
                case EventType.MouseMove:
                case EventType.MouseDrag:
                case EventType.Repaint:
                    break;

                default:
                    return;
            }

            var overlay = GetOverlay(guid);
            if (overlay == null)
                return;

            var color = GUI.color;
            GUI.color = overlay.Color;

            if (area.height > EditorGUIUtility.singleLineHeight)
            {
                area.height = 0.3f * (EditorGUIUtility.singleLineHeight + area.width);
            }
            else
            {
                area.x += area.width - area.height;

                const float Padding = 1;
                area.x += Padding;
                area.y += Padding;
                area.width -= Padding * 2;
                area.height -= Padding * 2;
            }

            area.width = area.height;

            HandleClicks(area, overlay);
            if (Event.current.type == EventType.Used)
                return;

            OverlayContent.image = overlay.IsOutOfDate
                ? LasSettings.OverlayAttentionIcon
                : LasSettings.OverlayIcon;
            OverlayContent.tooltip = overlay.Tooltip;

            if (_OverlayStyle == null)
            {
                _OverlayStyle = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(),
                };
            }

            GUI.Label(area, OverlayContent, _OverlayStyle);

            GUI.color = color;
        }

        /************************************************************************************************************************/

        private static Rect _TargetArea;

        private static void HandleClicks(Rect selectionArea, ProjectWindowOverlay overlay)
        {
            var currentEvent = Event.current;
            switch (currentEvent.type)
            {
                case EventType.MouseDown:// Capture target on start Right or Middle Click.
                    if ((currentEvent.button == 1 || currentEvent.button == 2) &&
                        selectionArea.Contains(currentEvent.mousePosition))
                    {
                        _TargetArea = selectionArea;
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseDrag:// Update target on Drag.
                    if (_TargetArea == selectionArea && !selectionArea.Contains(currentEvent.mousePosition))
                    {
                        _TargetArea.Set(0, 0, 0, 0);
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (selectionArea.Contains(currentEvent.mousePosition))
                    {
                        switch (currentEvent.button)
                        {
                            case 1:// Menu on release Right Click.
                                var menu = new GenericMenu();
                                MenuFunctions.AddFunctions(menu, overlay);
                                menu.ShowAsContext();
                                break;

                            case 2:// Sync on release Middle Click.
                                overlay.Sync();
                                break;
                        }

                        _TargetArea.Set(0, 0, 0, 0);
                        currentEvent.Use();
                    }
                    break;

                case EventType.Repaint:// Darken while holding Click.
                    if (_TargetArea == selectionArea && selectionArea.Contains(currentEvent.mousePosition))
                    {
                        var color = GUI.color;
                        color.r *= 0.5f;
                        color.g *= 0.5f;
                        color.b *= 0.5f;
                        GUI.color = color;
                    }
                    break;
            }
        }

        /************************************************************************************************************************/

        private static readonly List<LinkAndSync>
            Links = new List<LinkAndSync>();

        private static ProjectWindowOverlay GetOverlay(string guid)
        {
            if (GUIDToOverlay.TryGetValue(guid, out var overlay))
                return overlay;

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);

            Links.Clear();
            LinkAndSync.GetContainingLinks(ref assetPath, Links);

            if (Links.Count <= 0)
            {
                overlay = null;
            }
            else if (Links.Count == 1)
            {
                var link = Links[0];
                if (!SingleLinkOverlays.TryGetValue(link, out var single))
                {
                    single = new SingleLinkOverlay(link);
                    SingleLinkOverlays.Add(link, single);
                }
                overlay = single;
            }
            else
            {
                overlay = new MultiLinkOverlay(Links);
            }

            GUIDToOverlay.Add(guid, overlay);
            return overlay;
        }

        /************************************************************************************************************************/

        public abstract Color Color { get; }

        /************************************************************************************************************************/

        private string _Tooltip;

        public string Tooltip
        {
            get
            {
                if (_Tooltip == null)
                    _Tooltip = BuildTooltip();
                return _Tooltip;
            }
        }

        protected abstract string BuildTooltip();

        /************************************************************************************************************************/

        private class SingleLinkOverlay : ProjectWindowOverlay
        {
            private LinkAndSync _Link;

            public SingleLinkOverlay(LinkAndSync link)
            {
                _Link = link;
            }

            public override IEnumerator<LinkAndSync> GetEnumerator()
            {
                yield return _Link;
            }

            protected override string BuildTooltip()
            {
                if (_Link == null)
                    return null;

                return $"{_Link.name}\nRight Click for menu";
            }

            protected override bool IsOutOfDate
                => _Link.IsOutOfDate;

            protected override void Sync()
                => SyncOperationWindow.Show(new SyncOperation(_Link));

            public override Color Color
                => _Link.GetPrimaryColor();
        }

        /************************************************************************************************************************/

        private class MultiLinkOverlay : ProjectWindowOverlay
        {
            private new readonly List<LinkAndSync> Links;

            public MultiLinkOverlay(List<LinkAndSync> links)
            {
                Links = new List<LinkAndSync>(links);
            }

            public override IEnumerator<LinkAndSync> GetEnumerator()
                => Links.GetEnumerator();

            protected override string BuildTooltip()
            {
                TooltipBuilder.Length = 0;

                SyncDirection direction = default;

                foreach (var link in Links)
                {
                    if (link == null)
                        continue;

                    if (TooltipBuilder.Length == 0)
                    {
                        direction = link.Direction;
                    }
                    else
                    {
                        TooltipBuilder.Append(", ");
                        if (direction != link.Direction)
                            direction = ~default(SyncDirection);
                    }

                    TooltipBuilder.Append(link.name);
                }

                if (TooltipBuilder.Length == 0)
                {
                    TooltipBuilder.Append("No valid links here. This should never happen.");
                }
                else
                {
                    TooltipBuilder.Append("\nRight Click for menu");
                }

                var tooltip = TooltipBuilder.ToString();
                TooltipBuilder.Length = 0;
                return tooltip;
            }

            protected override bool IsOutOfDate
            {
                get
                {
                    foreach (var link in Links)
                        if (link.IsOutOfDate)
                            return true;

                    return false;
                }
            }

            protected override void Sync()
            {
                foreach (var link in Links)
                    SyncOperationWindow.Show(new SyncOperation(link));
            }

            public override Color Color
                => Color.white;
        }

        /************************************************************************************************************************/

        public static void ClearCache()
        {
            GUIDToOverlay.Clear();
            SingleLinkOverlays.Clear();

            EditorApplication.RepaintProjectWindow();
        }

        /************************************************************************************************************************/
    }
}

#endif