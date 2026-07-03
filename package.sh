# A helper script to package the mod for release. Usage: ./package.sh <version>
dotnet build
mkdir tmp
unzip "TileLocked/bin/Debug/net6.0/TileLocked $1.zip" -d tmp
rm "TileLocked/bin/Debug/net6.0/TileLocked $1.zip"
cd tmp/TileLocked
zip -r "../../TileLocked/bin/Debug/net6.0/TileLocked $1.zip" *
cd ../..
rm -r tmp