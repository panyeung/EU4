using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>Shows current-town identity and links to the existing roster and mission UI.</summary>
    [DisallowMultipleComponent]
    public sealed class TownOverviewPanel : MonoBehaviour
    {
        [SerializeField] private Image townIllustration;
        [SerializeField] private Text townNameText;
        [SerializeField] private Text townFeatureText;
        [SerializeField] private Text residentText;
        [SerializeField] private Text rumorText;
        [SerializeField] private Button rosterButton;
        [SerializeField] private Button missionButton;
        [SerializeField] private Button returnToMapButton;

        private WorldMapController _controller;

        public void BindReferences(Image illustration, Text townName, Text feature,
            Text residents, Text rumors, Button roster, Button mission, Button returnToMap)
        {
            BindListeners(false);
            townIllustration = illustration;
            townNameText = townName;
            townFeatureText = feature;
            residentText = residents;
            rumorText = rumors;
            rosterButton = roster;
            missionButton = mission;
            returnToMapButton = returnToMap;
            BindListeners(true);
            Refresh();
        }

        public void Bind(WorldMapController controller)
        {
            _controller = controller;
            Refresh();
        }

        public void Refresh()
        {
            GameSession session = _controller != null ? _controller.Session : null;
            if (session == null)
            {
                Set(townNameText, "城镇");
                Set(townFeatureText, "当前地点不可用。");
                Set(residentText, "居民\n暂无登记居民");
                Set(rumorText, "当地传闻\n暂无已验证的传闻");
                if (townIllustration != null)
                {
                    townIllustration.sprite = null;
                    townIllustration.color = new Color(0.2f, 0.24f, 0.29f, 0.12f);
                }
                if (rosterButton != null) rosterButton.interactable = false;
                if (missionButton != null) missionButton.interactable = false;
                return;
            }

            LocationId location = session.CurrentLocation;
            Set(townNameText, LocationId.GetChineseName(location));
            Set(townFeatureText, FeatureFor(location));
            if (townIllustration != null)
            {
                townIllustration.sprite = SengokuUiArt.TownIllustration(location);
                townIllustration.color = townIllustration.sprite != null
                    ? Color.white : new Color(0.2f, 0.24f, 0.29f, 0.12f);
            }

            var residents = TownNpcAssignments.GetNpcs(location.Value);
            if (residents.Count == 0)
            {
                Set(residentText, "当地人物\n暂无登记居民");
            }
            else
            {
                var names = new StringBuilder("当地人物");
                for (int i = 0; i < residents.Count; i++)
                {
                    string name = CharacterCatalog.TryGet(residents[i], out CharacterDefinition definition)
                        ? definition.DisplayName : residents[i].ToString();
                    names.Append('\n').Append(name);
                }
                Set(residentText, names.ToString());
            }

            Set(rumorText, "当地传闻\n暂无已验证的传闻");
            if (rosterButton != null)
                rosterButton.interactable = _controller.InTownMenu && residents.Count > 0;
            if (missionButton != null)
            {
                missionButton.interactable = _controller.InTownMenu && _controller.HasSupplyShortageMissionPanel;
                Text label = missionButton.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = _controller.IsSupplyShortageMissionOpen ? "返回城镇总览" : "任务委托";
            }
            if (returnToMapButton != null)
                returnToMapButton.interactable = _controller.InTownMenu;
        }

        private void OnEnable()
        {
            if (townIllustration != null)
                townIllustration.transform.SetAsFirstSibling();
            if (returnToMapButton != null)
                returnToMapButton.transform.SetAsLastSibling();
            BindListeners(true);
        }
        private void OnDisable() => BindListeners(false);

        private void BindListeners(bool add)
        {
            Bind(rosterButton, OpenRoster, add);
            Bind(missionButton, ToggleMission, add);
        }

        private void OpenRoster()
        {
            if (_controller != null) _controller.OpenNpcRoster();
        }

        private void ToggleMission()
        {
            if (_controller == null) return;
            _controller.ToggleSupplyShortageMissionPanel();
            if (missionButton == null || transform.parent == null) return;

            bool missionOpen = _controller.IsSupplyShortageMissionOpen;
            missionButton.transform.SetParent(missionOpen ? transform.parent : transform, true);
            if (missionOpen)
            {
                missionButton.transform.SetAsLastSibling();
                if (returnToMapButton != null)
                    returnToMapButton.transform.SetAsLastSibling();
            }
        }

        private static string FeatureFor(LocationId location)
        {
            if (location == LocationId.Kiyosu) return "尾张旧都 · 城郭与城门之间的街町";
            if (location == LocationId.Atsuta) return "热田神宫 · 参道连接港湾水岸";
            if (location == LocationId.Nagoya) return "那古野 · 沿路市集与丘陵边缘";
            return "尚无此地的城镇资料。";
        }

        private static void Set(Text target, string value)
        {
            if (target != null) target.text = value;
        }

        private static void Bind(Button button, UnityAction action, bool add)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (add) button.onClick.AddListener(action);
        }
    }
}
