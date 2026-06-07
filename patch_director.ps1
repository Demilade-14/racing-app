$path = 'c:\Users\AJAI MUHAMMED\Desktop\racing\Assets\Scripts\Race\RaceDirector.cs'
$c = [System.IO.File]::ReadAllText($path)

# 1. Add gapAhead/gapBehind to DriverEntry
$c = $c.Replace(
    'public float       distanceRaced;    // total metres',
    'public float       distanceRaced;    // total metres' + "`r`n" + '        public float       gapAhead  = 999f;' + "`r`n" + '        public float       gapBehind = 999f;'
)

# 2. Remove old SC private fields
$c = $c.Replace("        // Safety car`r`n`r`n        float _safetyCarTimer;`r`n        bool  _safetyCarDeployed;`r`n", "")

# 3. Remove TickSafetyCar(dt) call
$c = $c.Replace("            TickSafetyCar(dt);`r`n", "")

# 4. Remove DeploySafetyCar() call in RegisterRetirement
$c = $c.Replace("`r`n`r`n            // Possibly deploy safety car`r`n            if (Random.value < 0.35f) DeploySafetyCar();", "")

# 5. Remove DeploySafetyCar and TickSafetyCar method bodies
$old5 = "        void DeploySafetyCar()`r`n        {`r`n            if (_safetyCarDeployed) return;`r`n            _safetyCarDeployed = true;`r`n`r`n            _safetyCarTimer    = 120f;  // deploy for 2 laps approx`r`n            SetFlag(FlagStatus.SafetyCar);`r`n`r`n`r`n`r`n            DirectorState.safetyCarSpeed = 80f;`r`n`r`n`r`n            DirectorState.pitLaneOpen    = true;`r`n`r`n        }`r`n`r`n`r`n`r`n        void TickSafetyCar(float dt)`r`n`r`n`r`n        {`r`n`r`n            if (!_safetyCarDeployed) return;`r`n            _safetyCarTimer -= dt;`r`n            if (_safetyCarTimer <= 0f)`r`n`r`n            {`r`n`r`n                _safetyCarDeployed = false;`r`n`r`n                SetFlag(FlagStatus.Green);`r`n`r`n            }`r`n`r`n        }"
$c = $c.Replace($old5, "")

# 6. Add RaiseFlagChange after InitEntries
$old6 = "        public void InitEntries(List<DriverEntry> entries)`r`n        {`r`n`r`n            Entries = entries;`r`n        }`r`n    }"
$new6 = "        public void InitEntries(List<DriverEntry> entries)`r`n        {`r`n            Entries = entries;`r`n        }`r`n`r`n        public void RaiseFlagChange(FlagStatus flag)`r`n        {`r`n            DirectorState.flag = flag;`r`n            OnFlagChange?.Invoke(flag);`r`n        }`r`n    }"
$c = $c.Replace($old6, $new6)

[System.IO.File]::WriteAllText($path, $c)
Write-Host "Patch complete"
