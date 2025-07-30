# Nanover iMD

Interactive Molecular Dynamics (iMD) in VR, an application built with the NanoVer
framework.

This repository is maintained by the Intangible Realities Laboratory at the University of Santiago de Compostela,
and distributed under the [MIT](LICENSE) license.
See [the list of contributors](CONTRIBUTORS.md) for the individual authors of the project.

# Run the latest release build
* Download the [latest builds](https://github.com/IRL2/nanover-imd/releases).
* Extract the downloaded zip file.

* In the extracted directory,

## Android (Meta Quest etc)
* Sideload the `NanoVerIMD.apk` onto your device (you can use [SideQuest](https://sidequestvr.com/) for this).
* Look in the `Unknown Sources` section of your apps list and run NanoVer iMD.

## Windows (OpenXR / Meta Quest Link etc)
* In the extracted directory, launch `StandaloneWindows64.exe`. Windows will likely prompt you with a warning about the executable not being signed. If it happens, click on the "More info" button, then "Run anyway". You will also likely be prompted by the Windows firewall, allow NanoVer to access the network.

# Installation with conda

* Install Anaconda
* Open the "Anaconda Powershell Prompt" to enter the commands in the following instructions
* Create a conda environment (here we call the environment "nanover"): `conda create -n nanover "python>3.11"`
* Activate the conda environment: `conda activate nanover`
* Install the NanoVer IMD package: `conda install -c irl nanover-imd`
* Run the command `NanoverImd`

# Installation for Development

*  Clone this repository to a folder on your computer.
*  Download Unity Hub by visiting the [Unity Download Page](https://unity3d.com/get-unity/download) and clicking the green **Download Unity Hub** button.
*  Install Unity Hub onto your computer.
*  In Unity Hub, navigate to the **Projects** tab and click **Add** in the top right of Unity Hub.
*  Select the folder which you downloaded the repository to.
*  Install the version of Unity that Unity Hub tells you is required for this project.
*  Open the project in Unity and click "Restore Packages" in the NuGet menu.

Once open in Unity, the main Unity scene can be found in `NanoverImd/Assets/NanoverImd Scene`.

## Citation, Credits and External Libraries

Any work that uses NanoVer should cite the following publications:

> Stroud, H. J., Wonnacott, M. D., Barnoud, J., Roebuck Williams, R., Dhouioui, M., McSloy, A., Aisa, L., Toledo, L. E., Bates, P., Mulholland, A. J., & Glowacki, D. R. (2025). NanoVer Server: A Python Package for Serving Real-Time Multi-User Interactive Molecular Dynamics in Virtual Reality. *Journal of Open Source Software*, *10* (110), 8118. https://doi.org/10.21105/joss.08118

> Jamieson-Binnie, A. D., O’Connor, M. B., Barnoud, J., Wonnacott, M. D., Bennie, S. J., & Glowacki, D. R. (2020, August 17). Narupa iMD: A VR-Enabled Multiplayer Framework for Streaming Interactive Molecular Simulations. ACM SIGGRAPH 2020 Immersive Pavilion. SIGGRAPH ’20: Special Interest Group on Computer Graphics and Interactive Techniques Conference. https://doi.org/10.1145/3388536.3407891

> O’Connor, M., Bennie, S. J., Deeks, H. M., Jamieson-Binnie, A., Jones, A. J., Shannon, R. J., Walters, R., Mitchell, T., Mulholland, A. J., & Glowacki, D. R. (2019). Interactive molecular dynamics from quantum chemistry to drug binding: an open-source multi-person virtual reality framework, *The Journal of Chemical Physics*, *150* (22), 224703. https://doi.org/10.1021/acs.jcim.0c01030

This project has been made possible by the following projects. We gratefully thank them for their efforts, and suggest that you use and cite them:

* [unity](https://unity.com/) - Development platform.
* [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@2.1/manual/index.html) - Text rendering for Unity.
* [gRPC](https://grpc.io/) (Apache v2) - Communication protocol.
* The CIF file importer uses the *Chemical Component Dictionary* provided at http://www.wwpdb.org/data/ccd.
* [icons8](https://icons8.com) - Provider of icons used in UI.
* [NSubstitute](https://nsubstitute.github.io/) (BSD) - Mock testing framework.
