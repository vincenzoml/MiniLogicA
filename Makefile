.PHONY: test publish clean

test:
	@dotnet run testGraph.json

publish:
	dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true	
	dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
	dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
	mkdir -p publish
	mv bin/Release/netcoreapp3.0/linux-x64/publish/MiniLogicA publish/MiniLogicA-linux
	mv bin/Release/netcoreapp3.0/osx-x64/publish/MiniLogicA publish/MiniLogicA-osx
	mv bin/Release/netcoreapp3.0/win-x64/publish/MiniLogicA.exe publish/MiniLogicA-win

clean:
	rm -rf bin obj
