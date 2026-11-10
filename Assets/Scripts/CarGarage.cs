using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class CarGarage : MonoBehaviour
{
    [Serializable]
    public sealed class CarOption
    {
        public string id = "starter";
        public string displayName = "Starter";
        [Min(0)] public int unlockCost;
        public GameObject prefab;
    }

    [SerializeField] private CarOption[] cars;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Text garageText;

    private GameObject currentCar;
    private int selectedIndex;

    public GameObject SelectedPrefab => cars != null && cars.Length > 0 ? cars[selectedIndex].prefab : null;

    public int CarCount => cars != null ? cars.Length : 0;
    public int SelectedIndex => selectedIndex;

    private void Awake()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        if (cars != null && cars.Length > 0)
        {
            SaveManager.EnsureCarUnlocked(cars[0].id);
        }
    }

    private void Start()
    {
        if (cars == null || cars.Length == 0)
        {
            return;
        }

        SaveManager.EnsureCarUnlocked(cars[0].id);

        string savedCar = SaveManager.GetSelectedCar(cars[0].id);
        selectedIndex = Mathf.Max(0, FindCarIndex(savedCar));
        if (!SaveManager.IsCarUnlocked(cars[selectedIndex].id))
        {
            selectedIndex = 0;
        }

        SpawnSelectedCar();
        RefreshUi();
    }

    public void RefreshUi()
    {
        if (garageText == null || cars == null || cars.Length == 0)
        {
            return;
        }

        string firstCar = BuildCarLabel(0);
        string secondCar = cars.Length > 1 ? BuildCarLabel(1) : string.Empty;
        garageText.text = string.IsNullOrEmpty(secondCar)
            ? firstCar
            : $"{firstCar}\n{secondCar}";
    }

    private string BuildCarLabel(int index)
    {
        CarOption car = cars[index];
        bool unlocked = SaveManager.IsCarUnlocked(car.id);
        string key = (index + 1).ToString();
        string selected = index == selectedIndex ? "Selected" : $"Press {key}";

        if (unlocked)
        {
            return $"{key}. {car.displayName} - {selected}";
        }

        return $"{key}. {car.displayName} - Locked ({car.unlockCost} coins, press {key})";
    }

    public string GetCarDisplayName(int index)
    {
        return IsValidCarIndex(index) ? cars[index].displayName : string.Empty;
    }

    public int GetCarUnlockCost(int index)
    {
        return IsValidCarIndex(index) ? cars[index].unlockCost : 0;
    }

    public bool IsCarUnlocked(int index)
    {
        return IsValidCarIndex(index) && SaveManager.IsCarUnlocked(cars[index].id);
    }

    public bool TrySelectOrUnlock(int index, out string message)
    {
        message = string.Empty;
        if (levelManager != null && levelManager.RunInProgress)
        {
            message = "Finish or leave the current run before changing cars";
            return false;
        }
        if (!IsValidCarIndex(index))
        {
            message = "Car unavailable";
            return false;
        }

        CarOption car = cars[index];
        if (!SaveManager.IsCarUnlocked(car.id))
        {
            if (!SaveManager.TrySpendCoins(car.unlockCost))
            {
                message = "Insufficient Coins";
                if (levelManager != null)
                {
                    levelManager.ShowMessage(message);
                }

                RefreshUi();
                return false;
            }

            SaveManager.UnlockCar(car.id);
            message = $"{car.displayName} unlocked";
        }

        selectedIndex = index;
        SaveManager.SetSelectedCar(car.id);
        SpawnSelectedCar();

        if (string.IsNullOrEmpty(message))
        {
            message = $"{car.displayName} selected";
        }

        if (levelManager != null)
        {
            levelManager.RefreshTotalCoins();
            levelManager.ShowMessage($"{message}\nPress R to restart the current route");
        }

        RefreshUi();
        return true;
    }

    public void SetSpawnPoint(Transform newSpawnPoint, bool resetCurrentCar)
    {
        if (newSpawnPoint == null)
        {
            return;
        }

        spawnPoint = newSpawnPoint;
        transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        if (resetCurrentCar)
        {
            ResetCurrentCar();
        }
    }

    public void ResetCurrentCar()
    {
        if (currentCar == null || spawnPoint == null)
        {
            return;
        }

        CarController carController = currentCar.GetComponent<CarController>();
        if (carController != null)
        {
            carController.ResetCar(spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            currentCar.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }
    }

    private void SpawnSelectedCar()
    {
        if (cars[selectedIndex].prefab == null)
        {
            return;
        }

        if (currentCar != null)
        {
            Destroy(currentCar);
        }

        currentCar = Instantiate(cars[selectedIndex].prefab, spawnPoint.position, spawnPoint.rotation);
        currentCar.name = "PlayerCar";

        CarController carController = currentCar.GetComponent<CarController>();
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(currentCar.transform);
        }

        if (gameManager != null)
        {
            gameManager.SetPlayerCar(carController);
        }

        if (levelManager != null)
        {
            levelManager.SetPlayerCar(carController);
        }
    }

    private int FindCarIndex(string carId)
    {
        for (int i = 0; i < cars.Length; i++)
        {
            if (cars[i].id == carId)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsValidCarIndex(int index)
    {
        return cars != null && index >= 0 && index < cars.Length && cars[index] != null;
    }
}
