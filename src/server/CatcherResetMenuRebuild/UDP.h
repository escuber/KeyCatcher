#pragma once
#include <Arduino.h>
#include <WiFi.h>
#include <WiFiUdp.h>


static void StaTrialTask(void* param) {
  // take ownership of the heap-copy we passed in
  std::unique_ptr<std::vector<WifiCred>> creds((std::vector<WifiCred>*)param);

  bool connected = false;

  for (auto& c : *creds) {
    Serial.printf("Trying STA → %s\n", c.ssid.c_str());
    WiFi.begin(c.ssid.c_str(), c.pass.c_str());

    // wait up to ~6s (20 * 300ms)
    for (int t = 0; t < 20 && WiFi.status() != WL_CONNECTED; ++t) {
      vTaskDelay(pdMS_TO_TICKS(300));
      Serial.print('.');
    }
    Serial.println();

    if (WiFi.status() == WL_CONNECTED) {
      connected = true;
      Serial.printf("STA connected (IP=%s) on %s\n",
                    WiFi.localIP().toString().c_str(), c.ssid.c_str());

      // promote working pair so next boot is quick
      udpconfig.ssid     = c.ssid;
      udpconfig.password = c.pass;
      saveudpconfig();
      break;
    } else {
      Serial.println("…failed, trying next");
    }
  }

  // post-connect policy
  if (connected) {
    gStaConnected = true;

    if (!udpconfig.ap_mode) {
      // STA-only requested → shut down AP
      WiFi.softAPdisconnect(true);
      WiFi.mode(WIFI_STA);
      gApUp = false;
      Serial.println("AP disabled (STA is up)");
    } else {
      Serial.println("AP kept up (ap_mode = true)");
    }
  } else {
    gStaConnected = false;
    Serial.println("No STA link established.");
  }

  s_staTask = nullptr;
  vTaskDelete(nullptr);   // terminate this task
}

