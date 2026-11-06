using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    [System.Serializable]
    public class SkillUI
    {
        public string skillName;
        public Button qButton, eButton;
        public Text costText;
    }

    public Text infoText, hpCostText, spdCostText;
    public SkillUI[] skillUIs = new SkillUI[]
    {
        new SkillUI { skillName = "Homing" },
        new SkillUI { skillName = "TimeStop" },
        new SkillUI { skillName = "Reflect" },
        new SkillUI { skillName = "BlackHole" },
        new SkillUI { skillName = "SlowField" },
        new SkillUI { skillName = "Heal" }
    };
    public AudioClip shopBgm, buySfx;

    void Start()
    {
        Time.timeScale = 1;
        if (shopBgm) SoundManager.PlayBgm(shopBgm);
        UpdateUI();
    }

    public static int GetSkillCost(int lv) => 60 + lv * 40;

    public void BuyHp()
    {
        if (RunData.Money < 50) return;
        RunData.Money -= 50;
        RunData.HpLv++;
        SoundManager.PlaySfx(buySfx);
        UpdateUI();
    }

    public void BuySpeed()
    {
        if (RunData.Money < 50) return;
        RunData.Money -= 50;
        RunData.SpdLv++;
        SoundManager.PlaySfx(buySfx);
        UpdateUI();
    }

    public void BuyWing(string wingName)
    {
        if (RunData.Money < 100 || RunData.Owned.Contains(wingName)) return;
        RunData.Money -= 100;
        RunData.Owned.Add(wingName);
        RunData.Wing = wingName;
        SoundManager.PlaySfx(buySfx);
        UpdateUI();
    }

    public void BuySkillSlot0(string skillName) => UpgradeAndEquip(0, skillName);
    public void BuySkillSlot1(string skillName) => UpgradeAndEquip(1, skillName);

    void UpgradeAndEquip(int slot, string skillName)
    {
        int curLv = RunData.GetSkillLv(skillName);
        if (curLv >= 3)
        {
            DisableClickedButton();
            UpdateUI();
            return;
        }
        int cost = GetSkillCost(curLv);
        if (RunData.Money < cost) return;
        RunData.Money -= cost;
        int nextLv = curLv + 1;
        RunData.SkillLv[skillName] = nextLv;
        RunData.Slots[slot] = skillName;
        SoundManager.PlaySfx(buySfx);
        if (nextLv >= 3) DisableClickedButton();
        UpdateUI();
    }

    void DisableClickedButton()
    {
        var go = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (go && go.TryGetComponent<Button>(out var btn)) btn.interactable = false;
    }

    public void NextStage()
    {
        string target = (string.IsNullOrEmpty(RunData.GameScene) || RunData.GameScene == SceneManager.GetActiveScene().name)
            ? "Game"
            : RunData.GameScene;
        SceneManager.LoadScene(target);
    }

    void UpdateUI()
    {
        if (hpCostText) hpCostText.text = "비용 : 50 원";
        if (spdCostText) spdCostText.text = "비용 : 50 원";

        if (skillUIs != null)
        {
            foreach (var s in skillUIs)
            {
                if (s == null || string.IsNullOrEmpty(s.skillName)) continue;
                int lv = RunData.GetSkillLv(s.skillName);
                bool isMax = lv >= 3;
                if (s.qButton) s.qButton.interactable = !isMax;
                if (s.eButton) s.eButton.interactable = !isMax;
                if (s.costText)
                    s.costText.text = isMax ? "비용 : 구매완료" : $"비용 : {GetSkillCost(lv)} 원";
            }
        }

        if (!infoText) return;
        string s0 = RunData.Slots[0], s1 = RunData.Slots[1];
        int lv0 = RunData.GetSkillLv(s0), lv1 = RunData.GetSkillLv(s1);
        infoText.text = $"GOLD: {RunData.Money} | NEXT STAGE: {RunData.Stage}\n" +
                        $"HP LV: {RunData.HpLv} | SPD LV: {RunData.SpdLv} | WING: {RunData.Wing}\n" +
                        $"SKILL[Q]: {s0} (Lv.{lv0}/3) | SKILL[E]: {s1} (Lv.{lv1}/3)";
    }
}
