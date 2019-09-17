using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ModelSwitcher : MonoBehaviour
{
    [System.Serializable]
    public struct Model
    {
        public string name;
        public Transform prefab;
    }

    [System.Serializable]
    public struct RotationUI
    {
        public InputField eulerX;
        public InputField eulerY;
        public InputField eulerZ;
        public InputField quatX;
        public InputField quatY;
        public InputField quatZ;
        public InputField quatW;
    }

    [System.Serializable]
    public enum RotationComponent
    {
        EulerX, EulerY, EulerZ,
        QuatX, QuatY, QuatZ, QuatW
    };

    public enum InterpolationMode
    {
        Euler, Quat
    };

    [SerializeField]
    private Model[] _Models;
    [SerializeField]
    private RotationUI _StartRotUI;
    [SerializeField]
    private RotationUI _EndRotUI;
    [SerializeField]
    private Dropdown _ModelDropdown;
    [SerializeField]
    private Dropdown _ModeDropdown;
    [SerializeField]
    private Slider _InterpSlider;
    [SerializeField]
    private Text _InterpText;
    [SerializeField]
    private Transform _StartAxes;
    [SerializeField]
    private Transform _InterpAxes;
    [SerializeField]
    private Transform _EndAxes;
    [SerializeField]
    private Dropdown _RotateSideSelector;
    [SerializeField]
    private Dropdown _RotateAxisSelector;
    [SerializeField]
    private Dropdown _RotateRelativeSelector;

    private Quaternion _StartRot = Quaternion.identity;
    private Quaternion _EndRot = Quaternion.identity;
    private InterpolationMode _Mode;

    private bool isRotating = false;
    private bool rotationCW = false;

    public void Start()
    {
        InitRotationUI();
        OnModelDropdownUpdated();
    }

    public void Update()
    {
        if (isRotating)
        {
            Rotate(rotationCW);
        }
    }

    public void StartRotation(bool cw)
    {
        isRotating = true;
        rotationCW = cw;
    }

    public void EndRotation(bool cw)
    {
        if (rotationCW == cw)
        {
            isRotating = false;
        }
    }

    private void Rotate(bool cw)
    {
        bool left = _RotateSideSelector.value == 0;
        int axis = _RotateAxisSelector.value; // 0 == X, 1 == Y, 2 == Z
        bool world = _RotateRelativeSelector.value == 0;

        Quaternion rot = left ? _StartRot : _EndRot;
        RotationUI ui = left ? _StartRotUI : _EndRotUI;

        if (world)
        {
            var rotAround = Vector3.zero;
            rotAround[axis] = 1.0f;
            rot = Quaternion.AngleAxis(60.0f * Time.deltaTime * (cw ? -1 : 1), rotAround) * rot;
        } else
        {
            var ea = rot.eulerAngles;
            ea[axis] += 60.0f * Time.deltaTime * (cw ? -1 : 1);
            rot = Quaternion.Euler(ea);
        }

        if (left)
            _StartRot = rot;
        else
            _EndRot = rot;

        SetEulerFields(rot,ui);
        SetQuatFields(rot,ui);
        ApplyInterpolation(_InterpSlider.value);
    }

    public void InitRotationUI()
    {
        InitRotationUI(true, _StartRot, _StartRotUI);
        InitRotationUI(false, _EndRot, _EndRotUI);

        _ModelDropdown.onValueChanged.RemoveAllListeners();
        _ModelDropdown.onValueChanged.AddListener(opt => SetModel(_ModelDropdown.options[opt].text));

        _InterpSlider.onValueChanged.RemoveAllListeners();
        _InterpSlider.onValueChanged.AddListener(fl => _InterpText.text = "Interpolation: " + fl.ToString("0.00") + "/1.00");
        _InterpSlider.onValueChanged.AddListener(ApplyInterpolation);
        _InterpText.text = "Interpolation: " + _InterpSlider.value.ToString("0.00") + "/1.00";

        _Mode = _ModeDropdown.value == 0 ? InterpolationMode.Euler : InterpolationMode.Quat;
        ApplyInterpolation(_InterpSlider.value);

        _ModeDropdown.onValueChanged.RemoveAllListeners();
        _ModeDropdown.onValueChanged.AddListener(i => {
            _Mode = i == 0 ? InterpolationMode.Euler : InterpolationMode.Quat;
            ApplyInterpolation(_InterpSlider.value);
        });
    }

    private void SetEulerFields(Quaternion quat, RotationUI ui)
    {
        ui.eulerX.SetTextWithoutNotify(quat.eulerAngles.x.ToString());
        ui.eulerY.SetTextWithoutNotify(quat.eulerAngles.y.ToString());
        ui.eulerZ.SetTextWithoutNotify(quat.eulerAngles.z.ToString());
    }

    private void SetQuatFields(Quaternion quat, RotationUI ui)
    {
        ui.quatX.SetTextWithoutNotify(quat.x.ToString());
        ui.quatY.SetTextWithoutNotify(quat.y.ToString());
        ui.quatZ.SetTextWithoutNotify(quat.z.ToString());
        ui.quatW.SetTextWithoutNotify(quat.w.ToString());
    }

    private void InitRotationUI(bool start, Quaternion quat, RotationUI ui)
    {
        SetEulerFields(quat, ui);
        SetQuatFields(quat, ui);

        ui.eulerX.onValueChanged.RemoveAllListeners();
        ui.eulerY.onValueChanged.RemoveAllListeners();
        ui.eulerZ.onValueChanged.RemoveAllListeners();
        ui.quatX.onValueChanged.RemoveAllListeners();
        ui.quatY.onValueChanged.RemoveAllListeners();
        ui.quatZ.onValueChanged.RemoveAllListeners();
        ui.quatW.onValueChanged.RemoveAllListeners();

        ui.eulerX.onValueChanged.AddListener((str) => UpdateRotation(str, start, RotationComponent.EulerX));
        ui.eulerY.onValueChanged.AddListener((str) => UpdateRotation(str, start, RotationComponent.EulerY));
        ui.eulerZ.onValueChanged.AddListener((str) => UpdateRotation(str, start, RotationComponent.EulerZ));
        ui.quatX.onValueChanged.AddListener((str) => UpdateRotation(str, start, RotationComponent.QuatX));
        ui.quatY.onValueChanged.AddListener((str) => UpdateRotation(str, start, RotationComponent.QuatY));
        ui.quatZ.onValueChanged.AddListener((str) => UpdateRotation(str, start, RotationComponent.QuatZ));
        ui.quatW.onValueChanged.AddListener((str) => UpdateRotation(str, start, RotationComponent.QuatW));
    }

    public void ApplyInterpolation(float alpha)
    {
        if (_Mode == InterpolationMode.Euler)
        {
            var start = _StartRot.eulerAngles;
            var end = _EndRot.eulerAngles;
            transform.rotation = Quaternion.Euler(Vector3.Lerp(start, end, alpha));
        } else // quat
        {
            transform.rotation = Quaternion.Slerp(_StartRot, _EndRot, alpha);
        }

        _StartAxes.rotation = _StartRot;
        _InterpAxes.rotation = transform.rotation;
        _EndAxes.rotation = _EndRot;
    }

    public void UpdateRotation(string valueStr, bool start, RotationComponent component)
    {
        RotationUI ui = start ? _StartRotUI : _EndRotUI;
        Quaternion rotation = start ? _StartRot : _EndRot;
        var ea = rotation.eulerAngles;
        float value;
        if (!float.TryParse(valueStr, out value))
            return;

        switch (component)
        {
            case RotationComponent.EulerX:
                ea.x = value;
                rotation.eulerAngles = ea;
                SetQuatFields(rotation, ui);
                break;
            case RotationComponent.EulerY:
                ea.y = value;
                rotation.eulerAngles = ea;
                SetQuatFields(rotation, ui);
                break;
            case RotationComponent.EulerZ:
                ea.z = value;
                rotation.eulerAngles = ea;
                SetQuatFields(rotation, ui);
                break;
            case RotationComponent.QuatX:
                rotation.x = value;
                SetEulerFields(rotation, ui);
                break;
            case RotationComponent.QuatY:
                rotation.y = value;
                SetEulerFields(rotation, ui);
                break;
            case RotationComponent.QuatZ:
                rotation.z = value;
                SetEulerFields(rotation, ui);
                break;
            case RotationComponent.QuatW:
                rotation.w = value;
                SetEulerFields(rotation, ui);
                break;
        }

        if (start)
            _StartRot = rotation;
        else
            _EndRot = rotation;

        ApplyInterpolation(_InterpSlider.value);
    }

    public void OnModelDropdownUpdated()
    {
        SetModel(_ModelDropdown.options[_ModelDropdown.value].text);
    }

    public void SetModel(string name)
    {
        if (name == null)
            return;

        foreach(Model m in _Models) {
            if (name.ToLower().Equals(m.name.ToLower()))
            {
                foreach(Transform child in transform)
                    Destroy(child.gameObject);

                Instantiate(m.prefab, transform).localPosition = Vector3.zero;

                return;
            }
        }
    }
}
