using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using System;


public class ObjectPointer : MonoBehaviour
{

    [SerializeField]
    FurnitureSpawner m_FurnitureSpawner;

    [SerializeField]
    XRRayInteractor RightControllerRayInteractor;

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

                RightControllerRayInteractor.TryGetCurrent3DRaycastHit(out m_hit);
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
