# Robot Web Services (RWS) Communication between ABB IRB 120 (Server) and simple Client (C#)

## Requirements:

**Software:**
```bash
ABB RobotStudio, Visual Studio (or something similar)
```
ABB RS: https://new.abb.com/products/robotics/robotstudio/downloads

Visual Studio: https://visualstudio.microsoft.com/downloads/

**Programming Language:**
```bash
C#, Another Language (Python, C/C++ -> similar implementation as C#)
```

**Packages:**
```bash
C# (.NET Framework 4.6.1)
```

**Application Manual - Robot Web Services:**

Link: https://developercenter.robotstudio.com/api/rwsApi/index.html

## Project Description:

The project is focused on a simple demonstration of client / server communication via RWS (Robot Web Services). In this case, it is a industrial robot ABB IRB 120 (server), which communicates with the client via the C# application. An example of an application is reading data (Joint / Cartesian position) using multiple approaches (JSON,  XML). However, it is possible to use JSON to read the joint position and XML to read the Cartesian position, or to use both in one approach (XML / JSON to read joints and Cartesian position). The application was tested on some of the robot types (ABB IRB 120 -> real hardware + simulation, ABB IRB 1200, etc.)

The application uses performance optimization using multi-threaded programming. Communication (C# application) can be used in Unity3D for digital twins / augmented reality or in other relevant applications.

The project was realized at Institute of Automation and Computer Science, Brno University of Technology, Faculty of Mechanical Engineering (NETME Centre - Cybernetics and Robotics Division).

<p align="center">
<img src=https://github.com/rparak/ABB_Robot_data_processing/blob/main/images/communication_scheme.png width="650" height="350">
</p>

## Project Hierarchy:

**Client JSON (C#) - Repositary [/ABB_Robot_data_processing/ABB_RWS_JSON/ABB_RWS_XML_data_processing/]:**

```bash
[ Main Program ] /Program.cs/
```

**Client XML (C#) - Repositary [/ABB_Robot_data_processing/ABB_RWS_XML/ABB_RWS_XML_data_processing_sulution/]:**

```bash
[ Main Program ] /Program.cs/
```

## Contact Info:
Roman.Parak@outlook.com

## License
[MIT](https://choosealicense.com/licenses/mit/)
