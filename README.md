# Aeon_AssetDecDL
Use to download all Aeon Fantasy resources without actually play the game and decrypt files

With this tool you can easily download all Aeon Fantasy resources (exclude files in apk) without actually play the game.
This will be helpful for anyone who just wants the resources of the game.

And it can decrypt and unzip *.zip files with ZipCryto Deflate method.

eeab header encrypted files are also support to decrypt by this tools.

Note: Most of the code are copy from [KoK](https://github.com/28598519a/KoK_AssetDecDL)

## Usage
Easy 3 Steps, press the buttons in sequence

Note: 2022.2.1f1 is a very new version of asset bundle, you may have problem to extract it.

## Url
hash/latest -> assets1.json<br>
hash/request/latest -> assets2.json<br>
hash/secret/latest -> assets_secret.json **(Unable to download, lack of hash data)**

### Android
Asset list (MainWindow.xaml.cs#L31)
```
https://rp-cn-prod-server.rpfans.net/api/game-data/hash/latest?platform=android
https://rp-cn-prod-server.rpfans.net/api/game-data/hash/request/latest?platform=android
https://rp-cn-prod-server.rpfans.net/api/game-data/hash/secret/latest?platform=android
```
ServerURL (App.xaml.cs#L16)<br>
`https://d2o6bt848m9g7d.cloudfront.net/`

### Steam
Asset list (MainWindow.xaml.cs#L31)
```
https://rp-steam-prod-server.aeecho.com/api/game-data/hash/latest?platform=PC
https://rp-steam-prod-server.aeecho.com/api/game-data/hash/request/latest?platform=PC
https://rp-steam-prod-server.aeecho.com/api/game-data/hash/secret/latest?platform=PC
```
ServerURL (App.xaml.cs#L16)<br>
`https://rp-steam-prod-data-source.s3.amazonaws.com/`

**You should select one of them and modify the variables in the project before recompiling with Visual Studio**

## Sample
![image](https://github.com/28598519a/Aeon_AssetDecDL/assets/33422418/ff9f0520-98aa-4cf6-918b-1f5ab21f2849)
