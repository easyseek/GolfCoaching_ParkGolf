using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FilterHandler<TEnum> where TEnum : Enum
{
    private Toggle[] _toggles;
    private Toggle _allToggle;
    private ToggleGroup _group;
    private List<TEnum> _selectedList;
    private Action _onUpdate;

    public int SelectedCount {
        get
        {
            return _selectedList.Count; 
        }
    }

    public List<TEnum> SelectedList {
        get { return _selectedList; }
    }

    public FilterHandler(Toggle[] toggles, Toggle allToggle, ToggleGroup group, List<TEnum> selectedList, Action onUpdate)
    {
        _toggles = toggles;
        _allToggle = allToggle;
        _group = group;
        _selectedList = selectedList;
        _onUpdate = onUpdate;
    }

    public void OnValueChangedFilter(bool isOn)
    {
        _selectedList.Clear();

        foreach (var toggle in _toggles)
        {
            int value = toggle.GetComponent<UIValueObject>().intValue;

            TEnum enumValue = (TEnum)(object)value;

            if (toggle.isOn)
            {
                _selectedList.Add(enumValue);
            }
        }

        bool hasSelection = _selectedList.Count > 0;

        if(_group != null)
            _group.allowSwitchOff = hasSelection;

        if (_allToggle != null)
            _allToggle.isOn = !hasSelection;

        _onUpdate?.Invoke();
    }

    public void OnValueChangedAll(bool isOn)
    {
        if (!isOn)
            return;

        if (_allToggle == null)
            return;

        if(_group != null) 
            _group.allowSwitchOff = false;

        foreach (var toggle in _toggles)
        {
            toggle.isOn = false;
        }

        _selectedList.Clear();

        _onUpdate?.Invoke();
    }

    public void Reset()
    {
        if (_group != null)
            _group.allowSwitchOff = false;

        foreach (var toggle in _toggles)
        {
            toggle.isOn = false;
        }

        _selectedList.Clear();
    }
}
