import React from 'react'
import $IconPanel from '../components/_icon-panel';
import $CheckBox from '../components/_checkbox';
import $ToggleGroup from '../components/_toggle-group';

const $Settings = ({ data, setData, locale, triggerUpdate }) => {
    const react = window.$_gooee.react;
    const { Grid, CheckBox } = window.$_gooee.framework;

    const timeOfDayOptions = [
        { label: locale["OFF"], value: "Off" },
        { label: locale["DAY"], value: "Day" },
        { label: locale["GOLDEN_HOUR"], value: "GoldenHour" },
        { label: locale["NIGHT"], value: "Night" }
    ];

    const weatherOptions = [
        { label: locale["OFF"], value: "Off" },
        { label: locale["SUN"], value: "Sun" },
        { label: locale["OVERCAST"], value: "Overcast" },
        { label: locale["RAIN"], value: "Rain" },
        { label: locale["SNOW"], value: "Snow" }
    ];

    const timeOfDayUpdate = (val) => {
        let parsedVal = val;
        
        triggerUpdate("TimeOfDay", parsedVal);

        if (val == "Off")
            triggerUpdate("FreezeVisualTime", false);
        else
            triggerUpdate("FreezeVisualTime", true);

        // Optimistically update local state
        setData({ ...data, TimeOfDay: parsedVal, FreezeVisualTime: (val != "Off") });
    }

    const freezeTimeUpdate = (val) => {
        if (!val) {
            triggerUpdate("TimeOfDay", "Off");

            triggerUpdate("FreezeVisualTime", val);

            setData({ ...data, TimeOfDay: "Off", FreezeVisualTime: val });
        }
        else {
            triggerUpdate("FreezeVisualTime", val);
            setData({ ...data, FreezeVisualTime: val });
        }
    }

    const updateData = (field, val) => {
        if (field === "UseStickyWhiteness") {
            setData({ ...data, UseStickyWhiteness: val });
        }
        else if (field === "WhitenessToggle") {
            setData({ ...data, WhitenessToggle: val });
        }
        else if (field === "UseUnits") {
            setData({ ...data, UseUnits: val });
        }
        else if (field === "Weather") {
            setData({ ...data, Weather: val });
        }
        triggerUpdate(field, val);
    }
    
    return <Grid auto>
        <div>
            <div style={{ flex: 1, paddingRight: '5rem' }}>
                <$IconPanel label={locale["USE_STICKY_WHITENESS"]}
                    description={locale["USE_STICKY_WHITENESS_DESC"]}
                    icon="Media/Game/Icons/Information.svg">
                    <CheckBox style={{ alignSelf: 'center', margin: '10rem' }} checked={data.UseStickyWhiteness} onToggle={(val) => updateData("UseStickyWhiteness", val)} />
                </$IconPanel>
                <$IconPanel label={locale["WHITENESS_TOGGLE"]}
                    description={locale["WHITENESS_TOGGLE_DESC"]}
                    icon="Media/Game/Icons/Orbit.svg">
                    <CheckBox style={{ alignSelf: 'center', margin: '10rem' }} checked={data.WhitenessToggle} onToggle={(val) => updateData("WhitenessToggle", val)} />
                </$IconPanel>
                <$IconPanel label={locale["USE_UNITS"]}
                    description={locale["USE_UNITS_DESC"]}
                    icon="Media/Game/Icons/Roads.svg">
                    <CheckBox style={{ alignSelf: 'center', margin: '10rem' }} checked={data.UseUnits} onToggle={(val) => updateData("UseUnits", val)} />
                </$IconPanel>
            </div>
        </div>
        <div>
            <$IconPanel label={locale["FREEZE_TIME_VISUALS"]}
                description={locale["FREEZE_TIME_VISUALS_DESC"]}
                icon="Media/PhotoMode/Pause.svg">
                <CheckBox style={{ alignSelf: 'center', margin: '10rem' }} checked={data.FreezeVisualTime} onToggle={(val) => freezeTimeUpdate(val)} />
            </$IconPanel>
            <$IconPanel label={locale["SET_VISUAL_TIME_OF_DAY"]}
                description={locale["SET_VISUAL_TIME_OF_DAY_DESC"]}
                icon="Media/Editor/Time.svg" fitChild="true">
                <$ToggleGroup react={react} checked={data.TimeOfDay} options={timeOfDayOptions} isHorizontal="true" onChecked={(val) => timeOfDayUpdate(val)} />
            </$IconPanel>
            <$IconPanel label={locale["WEATHER"]}
                description={locale["WEATHER_DESC"]}
                icon="Media/Game/Climate/Overcast.svg" fitChild="true">
                <$ToggleGroup react={react} checked={data.Weather} options={weatherOptions} isHorizontal="true" onChecked={(val) => updateData("Weather", val)} />
            </$IconPanel>
        </div>
    </Grid>
}

export default $Settings