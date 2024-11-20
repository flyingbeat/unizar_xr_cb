using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;


public class ObjectPointer : MonoBehaviour
{

    [SerializeField]
    FurnitureSpawner m_FurnitureSpawner;

    [SerializeField]
    XRRayInteractor RightControllerRayInteractor;

    private RaycastHit Hit;
    private InputDevice rightHandDevice;
    private bool buttonHeld = false;

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
            //rightHandDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
            if (primaryButtonValue)
            {
                if (buttonHeld) return;

                Debug.Log("Primary button is pressed");
                RightControllerRayInteractor.TryGetCurrent3DRaycastHit(out Hit);
                GameObject CollidedObject = Hit.collider.gameObject;
                int CollidedObjectLayer = CollidedObject.layer;
                if (CollidedObjectLayer == LayerMask.NameToLayer("Changeable"))
                {
                    Debug.Log("Hit a changeable object");
                    Debug.Log(CollidedObject.name);
                }
                else if (CollidedObjectLayer == LayerMask.NameToLayer("Fixed"))
                {
                    Debug.Log("Hit a fixed object");
                    Debug.Log(CollidedObject.name);
                }
                else if (CollidedObjectLayer == LayerMask.NameToLayer("Planes"))
                {
                    Debug.Log("Hit a plane");
                    ARPlane plane = CollidedObject.GetComponent<ARPlane>();
                    Debug.Log(plane.classifications);
                }
                else if (CollidedObjectLayer == LayerMask.NameToLayer("BoundingBoxes"))
                {
                    Debug.Log("Hit a bounding box");
                }
                else
                {
                    Debug.Log("Hit something else");
                    Debug.Log(CollidedObject.name);
                }

                buttonHeld = true;
            }
            // else if (triggerValue > 0.1f)
            // {
            //     if (buttonHeld) return;
            //     Debug.Log("Trigger is pressed");
            //     RightControllerRayInteractor.TryGetCurrent3DRaycastHit(out Hit);
            //     m_FurnitureSpawner.TrySpawnAt(Hit.point, Quaternion.identity);
            //     buttonHeld = true;
            // }
            else
            {
                buttonHeld = false;
            }
        }
    }
}
