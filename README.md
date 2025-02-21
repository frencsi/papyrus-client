## 🌐 App  
You can find the latest release at [papyrus.frencsi.org](https://papyrus.frencsi.org).

> [!WARNING]
> Please note that the project is still in its initial stages.  
> There may be bugs and crashes, and significant changes are likely to happen.

## 📥 Building / Running  
> [!IMPORTANT]  
> The project uses [AOT compilation](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot?view=aspnetcore-9.0).  
> Please read the linked documentation for details.  

> Note: AOT compilation is performed only when the project is published.
```bash  
git clone https://github.com/frencsi/papyrus-client.git  
cd papyrus-client
dotnet run --project src\PapyrusClient  
```  

## 📖 Samples
Example Excel files can be found in the [samples](https://github.com/frencsi/papyrus-client/tree/main/samples) folder.

## 🌟 Roadmap  
- ✅ *Holiday support* – Allows selecting holidays in the options, which will later be used when processing Excel files.  
- ✅ *Language support* – Language can be selected in the options. The UI and generated files will reflect this.  
- ⌛ *Selectable (optionally custom) validation options* – Enables modification of Excel validation options.  
- ⌛ *Code quality* – Review and refactor areas where shortcuts were taken.  
- ⌛ *Code documentation* – Add comments and XML documentation.  
- ⌛ *Unit tests* – Add missing tests.  
- ⌛  *Multi-threading* **[champion]** – The very first version used [experimental multi-threading](https://github.com/dotnet/runtime/blob/main/src/mono/wasm/features.md#multi-threading), but it crashed due to high resource usage in constrained environments. This may be revisited later.  

## 📝 License
The code in this repo is licensed under the [MIT](https://github.com/frencsi/papyrus-client/blob/main/LICENSE) license.
