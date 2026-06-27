using System;
using System.IO;

class Program {
    static void Main() {
        // Base64 of a simple 16x16 red square with a black border PNG
        string base64 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAEFJREFUOE9jZKAQMFKon2HUAAZQYWBgYGAUFBRsZ2RkZMSpmQEJwI0nRQNYMIwawIDaYMDwH0j1M4wawABSw3+oHgAAn6oX0R497t0AAAAASUVORK5CYII=";
        byte[] imageBytes = Convert.FromBase64String(base64);
        File.WriteAllBytes("Game/MonoGameUI/player.png", imageBytes);
    }
}
