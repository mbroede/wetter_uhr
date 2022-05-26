# wetter_uhr
Visual Studio Projekt der Wetter-Uhr auf dem Raspberry

Voraussetzung zur Anzeige der Wetter-Uhr auf dem Raspberry Pi ist der Anschluss eines RGB-LED-Panels mit 64 x 64 Pixeln an die GPIO-Leiste. Die C#-Skripte dieses Projektes müssen auf den Raspberry Pi übertragen und dort zu einem ausführbaren Programm kompiliert werden. Dabei ist die LED-Panel-Bibliothek RGBLedMatrix.dll einzubinden. Auf dem Raspberry wird das Mono-Framework verwendet.
Wetterdaten werden via OpenWeather-API ermittelt (One-Call-Lizenz).
