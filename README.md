# AutoCruise
.Net vision based cruise and steer controller

InputProvider: OnFrontViewChange, OnRadarChange, OnPositionChange ("gps", orientation), OnStatusChange (speed, gear, ignition)  
InputProvider implementation: LfsInputProvider


OutputProvider: SetAcc, SetBrake, SetClutch, SetSteer, ChangeGearUp, ChangeGearDown, ChangeIgnitionSwitch)  
OutputProvider implementation: VJoyOutputProvider


DriveController: (inputProvider, outputProvider)  
DriveController implementation: AutoDrive, JoyProxy(bool: ALS)


main: display (InputProvider, DriveController, OutputProvider) data, switch between middleWares  
idea: any implementation or InputProvider, OutputProvider, DriveController will have a property named "Settings" deriving from "ISettings" implementation which will contain public properties to change/display in main view (use display name and group)
