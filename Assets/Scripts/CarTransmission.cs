using TMPro;
using UnityEngine;

public class CarTransmission : MonoBehaviour
{
    private CarBase car;

    [SerializeField] TextMeshProUGUI tempoInfo;

    [Header("Transmission")]
    [SerializeField] float[] gearRatios = { 3.5f, 2.2f, 1.5f, 1.1f, 0.85f };
    [SerializeField] float maxRPM = 6500f;
    [SerializeField] float shiftDownRPM = 2500f;

    [SerializeField] float finalDriveRatio = 3.4f;
    [SerializeField] float drivetrainEfficiency = 0.9f;

    private int currentGear = 0; // 0 = first gear
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        car = transform.GetComponent<CarBase>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGears();
        tempoInfo.text = $"RPM: {GetEngineRPM()}\nGEAR: {currentGear}\nSpeed: {gameObject.GetComponent<Rigidbody>().linearVelocity.magnitude} m/s\nWheel RPM: {car.wheels[2].rpm}\nWheel Torque: {car.wheels[2].motorTorque:F0}";
    }

    void GearUp()
    {
        if (currentGear < gearRatios.Length - 1) currentGear++;
    }
    void GearDown()
    {
        if (currentGear > 0) currentGear--;
    }

    float GetEngineRPM()
    {
        float wheelRPM = Mathf.Abs(car.wheels[2].rpm);

        return wheelRPM * gearRatios[currentGear] * finalDriveRatio;
    }

    void UpdateGears()
    {
        float rpm = GetEngineRPM();

        if (rpm > maxRPM && currentGear < gearRatios.Length - 1)
        {
            GearUp();
        }
        else if (rpm < shiftDownRPM && currentGear > 0)
        {
            GearDown();
        }
    }
    public void Accelerate(float torque) // A function that allows other scripts to "Floor the gas".
    {
        float wheelTorque = torque * gearRatios[currentGear] * finalDriveRatio * drivetrainEfficiency;

        int drivenWheels = car.wheels.Count - 2;

        for (int i = 2; i < car.wheels.Count; i++)
        {
            car.wheels[i].motorTorque = wheelTorque / drivenWheels;
        }
    }

    public void ReleaseGas()
    {
        for (int i = 2; i < car.wheels.Count; i++)
        {
            car.wheels[i].motorTorque = 0;
        }
    }
}
