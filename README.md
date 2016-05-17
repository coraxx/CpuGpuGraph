# README #

Cpu Gpu Graph is a little tool to monitor the CPU and GPU (only Nvidia) load. Nothing more. I wanted a simple and small app on my second monitor to keep an eye on my computer's performance. These are also my first real endeavors into C# and WPF.

![Cpu Gpu Graph screenshot](http://semper.space/CGG/Screenshot_01.png "Cpu Gpu Graph")

Used in this app:

* Material Design In XAML Toolkit [^1]
+ code from Chang-Hung Liang to get GPU load via nvapi.dll (installed with Nvidia driver) [^2]
+ the latter was packed by me into "nvGpuLoad_x86.dll"
+ WPF GUI

[^1]: http://materialdesigninxaml.net/
[^2]: http://eliang.blogspot.de/2011/05/getting-nvidia-gpu-usage-in-c.html

Download the binary or the Visual Studio solution if you are interested. Have fun.

### License ###

Copyright (C) 2016  Jan Arnold

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.

### Version ###

Version 1.0.0 - Initial Release

### Binaries ###

Download the portable version (zip file with the executable) or the the installer under the [download section](https://bitbucket.org/splo0sh/cpugpugraph/downloads).

### Who do I talk to? ###

* Repo owner or admin
* Other community or team contact