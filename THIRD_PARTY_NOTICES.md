# Third-party notices

The following components are included in the source tree or the self-contained
Windows executable. Versions were matched against Norbyte LSLib release
`v1.20.4` (commit `2746d7f01c4f0573c67a90c11ae09fb8663af0d5`) and the
corresponding NuGet package metadata.

## Component inventory

| Component | Version | License / copyright | Source |
| --- | ---: | --- | --- |
| LSLib | 1.20.4 | MIT; Copyright © Norbyte 2012–2023 | https://github.com/Norbyte/lslib/tree/v1.20.4 |
| K4os.Compression.LZ4 | 1.3.8 | MIT; Copyright © 2017 Milosz Krajewski | https://github.com/MiloszKrajewski/K4os.Compression.LZ4/tree/1.3.8 |
| K4os.Compression.LZ4.Streams | 1.3.8 | MIT; Copyright © 2017 Milosz Krajewski | https://github.com/MiloszKrajewski/K4os.Compression.LZ4/tree/1.3.8 |
| K4os.Hash.xxHash | 1.0.8 | MIT; Copyright © 2017 Milosz Krajewski | https://github.com/MiloszKrajewski/K4os.Hash.xxHash |
| Newtonsoft.Json | 13.0.3 | MIT; Copyright © 2007 James Newton-King | https://github.com/JamesNK/Newtonsoft.Json/tree/13.0.3 |
| OpenTK.Mathematics | 4.9.3 | MIT; Copyright © 2006–2019 Stefanos Apostolopoulos for the Open Toolkit project | https://github.com/opentk/opentk/tree/4.9.3 |
| SharpGLTF.Core / Runtime / Toolkit | 1.0.3 | MIT; Copyright © 2019 Vicente Penades | https://github.com/vpenades/SharpGLTF/tree/2803c34b2c2f2d7c2034e6633700c46447876a0f |
| ZstdSharp.Port | 0.8.5 | MIT; Copyright © 2021 Oleg Stepanischev | https://github.com/oleg-st/ZstdSharp/tree/033a60cc132a437ff64467fd0a531b45638ce2f2 |
| System.IO.Hashing | 9.0.2 | MIT; Copyright © .NET Foundation and Contributors | https://github.com/dotnet/runtime/tree/80aa709f5d919c6814726788dc6dabe23e79e672 |
| .NET 8 Windows Desktop Runtime | self-contained runtime | MIT; Copyright © .NET Foundation and Contributors | https://github.com/dotnet/runtime and https://github.com/dotnet/winforms |
| SixLabors.ImageSharp | 3.1.12 | Six Labors Split License 1.0 | https://github.com/SixLabors/ImageSharp/tree/4224257dccf1005973ae51de06993e4b3e502c21 |

The copied ExportTool files `System.IO.Hashing.dll` and
`System.IO.Pipelines.dll` are intentionally excluded from source control and
the application reference graph. The executable uses the audited package and
self-contained .NET 8 runtime implementations instead.

## MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The applicable copyright notice above and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## OpenTK third-party notice: OpenEXR

`OpenTK.Half` contains Half-to-Single and Single-to-Half conversions based on
OpenEXR source code.

Copyright © 2002, Industrial Light & Magic, a division of Lucas Digital Ltd.
LLC. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

- Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.
- Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
- Neither the name of Industrial Light & Magic nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

## Six Labors Split License

Version 1.0, June 2022
Copyright © Six Labors

### Terms and conditions for use, reproduction, and distribution

1. Definitions.

   "You" (or "Your") shall mean an individual or Legal Entity exercising
   permissions granted by this License.

   "Source" form shall mean the preferred form for making modifications,
   including but not limited to software source code, documentation source,
   and configuration files.

   "Object" form shall mean any form resulting from mechanical transformation
   or translation of a Source form, including but not limited to compiled
   object code, generated documentation, and conversions to other media types.

   "Work" (or "Works") shall mean any Six Labors software made available under
   the License, as indicated by a copyright notice that is included in or
   attached to the work.

   "Direct Package Dependency" shall mean any Work in Source or Object form
   that is installed directly by You.

   "Transitive Package Dependency" shall mean any Work in Object form that is
   installed indirectly by a third party dependency unrelated to Six Labors.

2. License.

   Works in Source or Object form are split licensed and may be licensed under
   the Apache License, Version 2.0 or a Six Labors Commercial Use License.

   Licenses are granted based upon You meeting the qualified criteria as
   stated. Once granted, You must reference the granted license only in all
   documentation.

   Works in Source or Object form are licensed to You under the Apache License,
   Version 2.0 if:

   - You are consuming the Work for use in software licensed under an Open
     Source or Source Available license.
   - You are consuming the Work as a Transitive Package Dependency.
   - You are consuming the Work as a Direct Package Dependency in the capacity
     of a for-profit company/individual with less than 1M USD annual gross
     revenue.
   - You are consuming the Work as a Direct Package Dependency in the capacity
     of a non-profit organization or registered charity.

   For all other scenarios, Works in Source or Object form are licensed to You
   under the Six Labors Commercial License, which may be purchased at
   https://sixlabors.com/pricing/.

Apache License 2.0 text: https://www.apache.org/licenses/LICENSE-2.0
