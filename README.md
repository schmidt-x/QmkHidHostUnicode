# QmkHidHostUnicode

## Table of Contents
- [Introduction](#introduction)
- [Configuration](#configuration)
	- [Alias](#alias)
  - [VendorId and ProductId](#vendorid-and-productid)
  - [UsagePage and UsageId](#usagepage-and-usageid)
  - [ReportId](#reportid)
  - [ReconnectInterval](#reconnectinterval)
  - [RepeatDelay and RepeatFrequency](#repeatdelay-and-repeatfrequency)
  - [MaxPressedKeys](#maxpressedkeys)
- [Usage](#usage)
  - [Report Signature](#report-signature)
  - [Examples](#examples)
    - [Custom keycodes](#custom-keycodes)
    - [Unicode Basic](#unicode-basic)
    - [Unicode Map](#unicode-map)

## Introduction
A simple, cross-platform console app for mapping raw HID reports into Unicode character input. The app acts as a host and listens for devices capable of sending raw, arbitrary reports (such as any keyboard powered by [Qmk Firmware](https://docs.qmk.fm/) using [Raw HID feature](https://docs.qmk.fm/features/rawhid)). These reports contain Unicode code points (along with some additional information), which the app translates into keystrokes using OS-specific APIs.

As a result, the app provides layout-independent input, supporting symbols and characters across the entire Unicode range, and additionally implements a customizable Key Repeat feature.

> [!NOTE]
> Currently, only Windows is supported.<br>
> Support for Linux and macOS is planned for future releases.

## Configuration

Define one or more devices in the following format:

```json
{
  "Devices": [
    {
      "Alias": "Device1",
      "VendorId": "0x0000",
      "ProductId": "0x0000",
      "UsagePage": "0xFF60",
      "UsageId": "0x61",
      "ReportId": "0x00",
      "ReconnectInterval": 3000,
      "RepeatDelay": 200,
      "RepeatFrequency": 15,
      "MaxPressedKeys": 6
    },
    {
      "Alias": "Device2",
      "VendorId": "0x0000",
      "ProductId": "0x0000",
      "ReportId": "0x00",
      "ReconnectInterval": 3000,
      "RepeatDelay": 200,
      "RepeatFrequency": 15,
      "MaxPressedKeys": 6
    }
  ]
}
```

### Alias

```json
{
  "Alias": "your_device_name"
}
```

Used for logging.

### VendorId and ProductId

```json
{
  "VendorId": "0x0000",
  "ProductId": "0x0000"
}
```
Device-specific values. On devices powered by [Qmk Firmware](https://docs.qmk.fm/), these values can be found at your keyboard's `info.json`, under the `usb` object.
Alternatively, you can use Device Manager on Windows, System Information on macOS, or `lsusb` on Linux.

### UsagePage and UsageId

```json
{
  "UsagePage": "0xFF60",
  "UsageId": "0x61"
}
```

Default values for [Raw HID](https://docs.qmk.fm/features/rawhid#basic-configuration) interface. Unless modified on the device side, can be omitted.

### ReportId

```json
{
  "ReportId": 0
}
```

The first byte of an input report must match this value. Otherwise, the report is ignored.

Allowed values are 0-255.

### ReconnectInterval

```json
{
  "ReconnectInterval": 3000
}
```

The number of milliseconds between attempts to find the device or reconnect to it if it gets disconnected.

### RepeatDelay and RepeatFrequency

```json
{
  "RepeatDelay": 200,
  "RepeatFrequency": 15
}
```

RepeatDelay: the number of milliseconds that the user must hold down a key before it begins to repeat.

RepeatFrequency: the number of milliseconds between each repetition of the keystroke.

> [!NOTE]
> Due to the granularity of the OS's time-keeping system, both values are typically rounded up to the nearest multiple of 10 or 15.6 milliseconds (depending on the type of hardware and drivers installed).

### MaxPressedKeys

```json
{
  "MaxPressedKeys": 6
}
```

The number of simultaneously pressed keys to remember. If the limit is exceeded, the oldest key is removed. When the most recently pressed key is released, the previously overlapped key (if any) is repeated after [RepeatDelay](#repeatdelay-and-repeatfrequency) milliseconds.

## Usage

### Report signature

The signature of a report is the following:
```
report[0] = report Id
      [1] = codepoint (0xFF0000)
      [2] = codepoint (0x00FF00)
      [3] = codepoint (0x0000FF)
      [4] = key state (0: Up; 1: Down)
```

### Examples

> [!NOTE]
> The following examples are based on the [Raw HID feature](https://docs.qmk.fm/features/rawhid) mentioned earlier.<br> 
> Please refer to the documentation for your device's firmware for the corresponding details.

Helper function that's used across the examples:

```c
#include "raw_hid.h"

#define HID_UNICODE_REPORT_ID 0x01

void raw_hid_send_unicode(uint32_t codepoint, bool is_pressed) {
  uint8_t report[32];
  memset(report, 0, 32);
  
  report[0] = HID_UNICODE_REPORT_ID;
  report[1] = (codepoint >> 16) & 0xFF;
  report[2] = (codepoint >> 8) & 0xFF;
  report[3] = codepoint & 0xFF;
  report[4] = (uint8_t)is_pressed;
  
  raw_hid_send(report, 32);
}
```

Basically, there are 3 ways to use Unicode keys:
- [Custom keycodes](https://docs.qmk.fm/custom_quantum_functions#custom-keycodes)
- [Unicode Basic](https://docs.qmk.fm/features/unicode#input-subsystems) (tab «Basic»)
- [Unicode Map](https://docs.qmk.fm/features/unicode#input-subsystems) (tab «Unicode Map»)

#### Custom keycodes

```c
enum custom_keycodes {
  OMEGA = SAFE_RANGE,
  AMONGOOS,
  POOP
};


bool process_record_user(uint16_t keycode, keyrecord_t *record) {
  switch (keycode) {
    case OMEGA: // Ω
      raw_hid_send_unicode(0x03A9, record->event.pressed);
      return false;
      
    case AMONGOOS: // ඞ
      raw_hid_send_unicode(0x0D9E, record->event.pressed);
      return false;
    
    case POOP: // 💩
      raw_hid_send_unicode(0x1F4A9, record->event.pressed);
      return false;
    
    // etc...
    
    default:
      return true; // Process all other keycodes normally
  }
}
```

#### Unicode Basic

```c
bool process_record_user(uint16_t keycode, keyrecord_t *record) {
  switch (keycode) {
    case QK_UNICODE ... QK_UNICODE_MAX: {
    	raw_hid_send_unicode(QK_UNICODE_GET_CODE_POINT(keycode), record->event.pressed);
    	return false;
    }
    
    default: 
    	return true; // Process all other keycodes normally
  }
}
```

#### Unicode Map

```c
enum unicode_names {
  OMEGA,
  AMONGOOS,
  POOP
};

const uint32_t PROGMEM unicode_map[] = {
  [OMEGA]    = 0x03A9,  // Ω
  [AMONGOOS] = 0x0D9E,  // ඞ
  [POOP]     = 0x1F4A9, // 💩
};


bool process_record_user(uint16_t keycode, keyrecord_t *record) {
  switch (keycode) {
    case QK_UNICODEMAP ... QK_UNICODEMAP_MAX:
      raw_hid_send_unicode(unicode_map[QK_UNICODEMAP_GET_INDEX(keycode)], record->event.pressed);
      return false;
    
    default:
      return true; // Process all other keycodes normally
  }
}
```

> [!NOTE]
> Note that in both Unicode examples, the feature itself is not required to be turned on.