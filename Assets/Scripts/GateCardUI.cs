using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class GateCardUI : MonoBehaviour
{
    [Header("Images")]
    public Image imgRank;
    public Image imgGate;
    public Image imgFrame;

    [Header("Currency Icons (assign in Inspector)")]
    public Image imgEnergyIcon;
    public Image imgGoldIcon;
    public Image imgEssenceIcon;

    [Header("Texts")]
    public TMP_Text txtDuration;
    public TMP_Text txtEnergy;
    public TMP_Text txtGold;
    public TMP_Text txtXP;
    public TMP_Text txtEssence;

    [Header("Button")]
    public Button btnAccept;

    [Header("Sprite Sets (ScriptableObjects)")]
    public RankSpriteSet rankSprites;
    public GateSpriteSet gateSprites;

    [Header("Gate Rotation")]
    public float gateRotateSpeed = 90f;

    int index;
    GateManager gateManager;

    public void Setup(GateData gate, int idx, GateManager manager)
    {
        index = idx;
        gateManager = manager;

        if (txtDuration) txtDuration.text = $"{gate.durationSeconds}s";
        if (txtEnergy) txtEnergy.text = $"{gate.energyCost}";
        if (txtGold) txtGold.text = $"{gate.rewardGold}";
        if (txtXP) txtXP.text = $"{gate.rewardXP}";
        if (txtEssence) txtEssence.text = $"{gate.rewardEssence}";

        if (imgRank != null && rankSprites != null)
            imgRank.sprite = rankSprites.Get(gate.rank);

        if (gateSprites != null && gateSprites.TryGet(gate.rank, out var entry))
        {
            if (imgGate != null) imgGate.sprite = entry.gateSprite;
            if (imgFrame != null)
                imgFrame.sprite = (gate.rank == GateRank.SRank) ? entry.frameSRankSprite : entry.frameNormalSprite;
        }

        if (btnAccept != null)
        {
            btnAccept.onClick.RemoveAllListeners();
            btnAccept.onClick.AddListener(OnAccept);
            btnAccept.interactable = (gateManager != null && gateManager.activeGate == null);
        }
    }

    void Update()
    {
        if (imgGate != null && imgGate.gameObject.activeInHierarchy)
            imgGate.rectTransform.Rotate(0f, 0f, gateRotateSpeed * Time.deltaTime);
    }

    void OnAccept()
    {
        if (gateManager != null)
            gateManager.AcceptGate(index);
    }
}
