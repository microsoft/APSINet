REM Parameters:
REM %1 - vcpkg install directory
REM %2 - build type: Release/Debug

cd ..\..\external\APSI
cmake -S . -B build\%2 -DAPSI_USE_CXX17=ON -DAPSI_BUILD_CLI=OFF -DAPSI_BUILD_TESTS=OFF -DAPSI_USE_ZMQ=OFF -DCMAKE_BUILD_TYPE=%2 -DCMAKE_TOOLCHAIN_FILE=%1\scripts\buildsystems\vcpkg.cmake -DVCPKG_TARGET_TRIPLET=x64-windows-static-md
cmake --build build\%2 --config %2


