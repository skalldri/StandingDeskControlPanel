# StandingDeskControlPanel
The UWP App for controlling the IoT standing desk. This app will implement the Cortana controls as well as the calibration UI.

The Cortana Skills Kit has still not been released, so currently the only way to get Cortana integration is to write a UWP app which registers itself as a Cortana endpoint.

Given that this will generally be used at my PC, this is fine. In future I would like to convert the Cortana element into an Alexa-style Skill which lives in the cloud. This would allow it to be activated from any Cortana device registered to the Microsoft Account (for example, a HoloLens could make the request without needing the Standing Desk App installed, as could an Android phone running the Cortana app).
