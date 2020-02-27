using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions.ColorPicker;

public class ColorButton : MonoBehaviour {
	public ColorChangedEvent OnColorChanged = new ColorChangedEvent();

	// Use this for initialization
	void Start () {
		var colorPicker = transform.root.GetComponentInChildren<ColorPickerControl>(true);
		var image = GetComponent<Image>();
		GetComponent<Button>().onClick.AddListener(() =>
		{
			colorPicker.gameObject.SetActive(true);
			colorPicker.CurrentColor = image.color;
			colorPicker.onValueChanged.AddListener(col =>
			{
				image.color = col;
				OnColorChanged.Invoke(col);
			});
		});
	}
}
