using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

using static UnityEngine.Mathf;

[ExecuteInEditMode, RequireComponent(typeof(Image))]
public class ColorPicker : UIBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IMaterialModifier
{
    private const float Recip2Pi = 0.5f / PI;
    private const string ColorPickerShaderName = "UI/ColorPicker";

    private static readonly int _HSV = Shader.PropertyToID(nameof(_HSV));
    private static readonly int _AspectRatio = Shader.PropertyToID(nameof(_AspectRatio));
    private static readonly int _HueCircleInner = Shader.PropertyToID(nameof(_HueCircleInner));
    private static readonly int _HueSelectorInner = Shader.PropertyToID(nameof(_HueSelectorInner));
    private static readonly int _SVSquareSize = Shader.PropertyToID(nameof(_SVSquareSize));

    [SerializeField, Range(0, 0.5f)] private float _hueCircleInnerRadius = 0.4f;
    [SerializeField, Range(0, 1)] private float _hueSelectorInnerRadius = 0.8f;
    [SerializeField, Range(0, 0.5f)] private float _saturationValueSquareSize = 0.25f;

    [SerializeField, FormerlySerializedAs("colorPickerShader")] private Shader _colorPickerShader;
    private Material _generatedMaterial;

    private enum PointerDownLocation { HueCircle, SVSquare, Outside }
    private PointerDownLocation _pointerDownLocation = PointerDownLocation.Outside;

    private RectTransform _rectTransform => (RectTransform)transform;

    private float _h, _s, _v;

    public Color color
    {
        get => Color.HSVToRGB(_h, _s, _v);
        set
        {
            Color.RGBToHSV(value, out _h, out _s, out _v);
            ApplyColor();
        }
    }

    public event Action<Color> onColorChanged;

#if UNITY_EDITOR
    protected override void Reset()
    {
        base.Reset();

        _colorPickerShader = Shader.Find(ColorPickerShaderName);
    }
#endif

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        UpdateAspectRatio();
    }

    private void UpdateAspectRatio()
    {
        if (_generatedMaterial != null)
        {
            var rect = _rectTransform.rect;

            _generatedMaterial.SetFloat(_AspectRatio, rect.width / rect.height);
        }
    }

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (_generatedMaterial == null)
        {
            _generatedMaterial = new Material(_colorPickerShader);
            _generatedMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        UpdateAspectRatio();
        ApplyColor();
        ApplySizesOfElements();

        return _generatedMaterial;
    }

    public void ApplySizesOfElements()
    {
        if (_generatedMaterial != null)
        {
            _generatedMaterial.SetFloat(_HueCircleInner, _hueCircleInnerRadius);
            _generatedMaterial.SetFloat(_HueSelectorInner, _hueSelectorInnerRadius);
            _generatedMaterial.SetFloat(_SVSquareSize, _saturationValueSquareSize);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        var relativePosition = GetRelativePosition(eventData);

        if (_pointerDownLocation == PointerDownLocation.HueCircle)
        {
            _h = (Atan2(relativePosition.y, relativePosition.x) * Recip2Pi + 1) % 1;
            ApplyColor();
        }

        if (_pointerDownLocation == PointerDownLocation.SVSquare)
        {
            var size = _generatedMaterial.GetFloat(_SVSquareSize);

            _s = InverseLerp(-size, size, relativePosition.x);
            _v = InverseLerp(-size, size, relativePosition.y);
            ApplyColor();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var relativePosition = GetRelativePosition(eventData);

        float r = relativePosition.magnitude;

        if (r < 0.5f && r > _generatedMaterial.GetFloat(_HueCircleInner))
        {
            _pointerDownLocation = PointerDownLocation.HueCircle;
            _h = (Atan2(relativePosition.y, relativePosition.x) * Recip2Pi + 1) % 1;
            ApplyColor();
        }
        else
        {
            var size = _generatedMaterial.GetFloat(_SVSquareSize);

            // s -> x, v -> y
            if (relativePosition.x >= -size && relativePosition.x <= size && relativePosition.y >= -size && relativePosition.y <= size)
            {
                _pointerDownLocation = PointerDownLocation.SVSquare;
                _s = InverseLerp(-size, size, relativePosition.x);
                _v = InverseLerp(-size, size, relativePosition.y);
                ApplyColor();
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pointerDownLocation = PointerDownLocation.Outside;
    }

    private void ApplyColor()
    {
        _generatedMaterial.SetVector(_HSV, new Vector3(_h, _s, _v));

        onColorChanged?.Invoke(color);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_generatedMaterial != null)
        {
            DestroyImmediate(_generatedMaterial);
        }
    }

    /// <summary>
    /// Returns position in range -0.5..0.5 when it's inside color picker square area
    /// </summary>
    public Vector2 GetRelativePosition(PointerEventData eventData)
    {
        var rect = GetSquaredRect();

        Vector2 rtPos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out rtPos);

        return new Vector2(InverseLerpUnclamped(rect.xMin, rect.xMax, rtPos.x), InverseLerpUnclamped(rect.yMin, rect.yMax, rtPos.y)) - Vector2.one * 0.5f;
    }

    public Rect GetSquaredRect()
    {
        var rect = _rectTransform.rect;
        var smallestDimension = Min(rect.width, rect.height);
        return new Rect(rect.center - Vector2.one * smallestDimension * 0.5f, Vector2.one * smallestDimension);
    }

    public float InverseLerpUnclamped(float min, float max, float value)
    {
        return (value - min) / (max - min);
    }
}