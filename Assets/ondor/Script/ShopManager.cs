using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    public Text infoText;
    public AudioClip shopBgm, buySfx;

    void Start()
    {
        Time.timeScale = 1;
        if (shopBgm) SoundManager.PlayBgm(shopBgm);
        UpdateUI();
    }

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
            RunData.Slots[slot] = skillName;
            UpdateUI();
            return;
        }
        int cost = 60 + curLv * 40;
        if (RunData.Money < cost) return;
        RunData.Money -= cost;
        RunData.SkillLv[skillName] = curLv + 1;
        RunData.Slots[slot] = skillName;
        SoundManager.PlaySfx(buySfx);
        UpdateUI();
    }

    public void NextStage()
    {
        SceneManager.LoadScene(RunData.GameScene);
    }

    void UpdateUI()
    {
        if (!infoText) return;
        string s0 = RunData.Slots[0], s1 = RunData.Slots[1];
        int lv0 = RunData.GetSkillLv(s0), lv1 = RunData.GetSkillLv(s1);
        infoText.text = $"GOLD: {RunData.Money} | NEXT STAGE: {RunData.Stage}\n" +
                        $"HP LV: {RunData.HpLv} | SPD LV: {RunData.SpdLv} | WING: {RunData.Wing}\n" +
                        $"SKILL[Q]: {s0} (Lv.{lv0}/3) | SKILL[E]: {s1} (Lv.{lv1}/3)";
    }
}
