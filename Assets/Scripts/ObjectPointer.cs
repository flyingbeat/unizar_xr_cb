using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR;
using System;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class ObjectPointer : MonoBehaviour
{

    [SerializeField]
    XRRayInteractor m_defaultRayInteractor;


    [SerializeField]
    XRRayInteractor m_hiddenLayerRayInteractor;

    public event Action<RaycastHit> ButtonPressed;
    private RaycastHit m_hit;
    public RaycastHit Hit { get { return m_hit; } }
    private InputDevice rightHandDevice;
    private bool m_buttonHeld = false;
    public bool buttonHeld { get { return m_buttonHeld; } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CheckControllers();
    }

    private void CheckControllers()
    {
        var rightHandedControllers = new List<InputDevice>();
        var desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, rightHandedControllers);

        if (rightHandedControllers.Count == 1)
        {
            InputDevice device = rightHandedControllers[0];
            Debug.Log(string.Format("Device name '{0}' with role '{1}'", device.name, device.characteristics.ToString()));
            rightHandDevice = device;
        }
        else if (rightHandedControllers.Count > 1)
        {
            Debug.Log("Found more than one right hand!");
        }
    }

    public bool ControllersInHand()
    {
        if (rightHandDevice.isValid)
        {
            return true;
        }
        CheckControllers();
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (rightHandDevice.isValid)
        {
            rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonValue);
            if (primaryButtonValue)
            {
                if (m_buttonHeld) return;

                bool hitOnDefaultLayer = m_defaultRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit defaultLayerHit);
                bool hitOnHiddenLayer = m_hiddenLayerRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hiddenLayerHit);
                m_hit = hitOnHiddenLayer ? hiddenLayerHit : defaultLayerHit;
                ButtonPressed?.Invoke(m_hit);

                m_buttonHeld = true;
            }
            else
            {
                m_buttonHeld = false;
            }
        }
    }
}
