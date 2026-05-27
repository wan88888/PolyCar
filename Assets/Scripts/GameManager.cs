using UnityEngine;
using UnityEngine.UI;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private CarController playerCar;
    [SerializeField] private Text speedText;
    [SerializeField] private Text driftStateText;
    [SerializeField] private Color normalColor = new Color(0.92f, 0.96f, 1f);
    [SerializeField] private Color driftColor = new Color(1f, 0.78f, 0.28f);

    private void Awake()
    {
        if (playerCar == null)
        {
            playerCar = FindFirstObjectByType<CarController>();
        }
    }

    private void Update()
    {
        if (playerCar == null)
        {
            return;
        }

        if (speedText != null)
        {
            speedText.text = $"{playerCar.SpeedKmh:0} km/h";
        }

        if (driftStateText != null)
        {
            driftStateText.text = playerCar.IsDrifting ? "Drift" : "Normal";
            driftStateText.color = playerCar.IsDrifting ? driftColor : normalColor;
        }
    }

    public void SetPlayerCar(CarController car)
    {
        playerCar = car;
    }
}
