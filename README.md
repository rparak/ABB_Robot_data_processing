# Robot Web Services (RWS) Communication between ABB IRB 120 (Server) and simple Client (C#)

## Requirements:

**Software:**
```bash
ABB RobotStudio, Visual Studio (or something similar)
```

| Software/Package      | Link                                                                                  |
| --------------------- | ------------------------------------------------------------------------------------- |
| ABB RobotStudio       | https://new.abb.com/products/robotics/robotstudio/downloads                                                     |
| Visual Studio         | https://visualstudio.microsoft.com/downloads/                                         |

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

The project is focused on a simple demonstration of client-server communication via RWS (Robot Web Services). In this case, it is a industrial robot ABB IRB 120 (server), which communicates with the client via the C# application. An example of an application is reading data (Joint / Cartesian position) using multiple approaches (JSON,  XML). However, it is possible to use JSON to read the joint position and XML to read the Cartesian position, or to use both in one approach (XML / JSON to read joints and Cartesian position). The application was tested on some of the robot types (ABB IRB 120 -> real hardware + simulation, ABB IRB 1200, etc.)

The application uses performance optimization using multi-threaded programming. Communication (C# application) can be used in Unity3D for digital twins / augmented reality or in other relevant applications.

Sample application in the Unity3D program (Digital-Twin):

[ABB IRB 120 - Unity3D Robotics](https://github.com/rparak/Unity3D_Robotics_ABB)

The project was realized at Institute of Automation and Computer Science, Brno University of Technology, Faculty of Mechanical Engineering (NETME Centre - Cybernetics and Robotics Division).

<p align="center">
<img src=https://github.com/rparak/ABB_Robot_data_processing/blob/main/images/communication_scheme.png width="650" height="350">
</p>

## Project Hierarchy:

**Client JSON (C#) - Repositary [/ABB_Robot_data_processing/ABB_RWS_JSON/]:**

```bash
[ Main Program ] /Program.cs/
```

**Client XML (C#) - Repositary [/ABB_Robot_data_processing/ABB_RWS_XML/]:**

```bash
[ Main Program ] /Program.cs/
```

## Example of reading Joint position and Cartesian position using different approaches (ABB IRB 120):

<p align="center">
<img src=https://github.com/rparak/ABB_Robot_data_processing/blob/main/images/abb_1.PNG width="650" height="350">
<img src=https://github.com/rparak/ABB_Robot_data_processing/blob/main/images/abb_2.PNG width="650" height="350">
</p>

## Contact Info:
Roman.Parak@outlook.com

## Citation (BibTex)
```bash
@misc{RomanParak_DT_ABB,
  author = {Roman Parak},
  title = {Data collection from the ABB controller using Robot Web Services (RWS)},
  year = {2020-2021},
  publisher = {GitHub},
  journal = {GitHub repository},
  howpublished = {\url{https://github.com/rparak/ABB-RobotStudio-YUMI/}}
}
```

## License
[MIT](https://choosealicense.com/licenses/mit/)
