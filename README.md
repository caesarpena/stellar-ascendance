# Stellar Ascendance – Media Upload, Thumbnailing & Library

End-to-end stack for uploading media, auto-generating thumbnails via **Azure Functions** (Event Grid trigger + FFmpeg), and managing a user’s media library through an **ASP.NET Core Web API** with an **Angular (Tailwind)** UI.

```
/api        → ASP.NET Core Web API (JWT-secured + API key endpoint)
/functions  → Azure Functions (Event Grid trigger → FFmpeg → thumbnail upload → API notify)
/ui         → Angular app (Tailwind) for upload & browsing
```

---

## Overview

**Flow**

```
[UI] --(multipart/form-data)--> [API] POST /api/UploadEncodeAndStreamFiles/upload-file
   ↳ API streams file to Azure Blob Storage: input-{userId}/<filename>

[Azure Storage] --(Event Grid: BlobCreated)--> [Azure Function] (EventGridTrigger)
   ↳ downloads the new blob
   ↳ (videos) runs FFmpeg (frame @ 00:00:02, size 120x80)
   ↳ uploads thumbnail → thumbnails/<name>_thumbnail.png (same container)
   ↳ POSTs { videoAzureUrl, thumbnailAzureUrl } to
      /api/MediaItems/add-media-item-thumbnail  (header: X-Api-Key)

[API]
   ↳ stores/updates the DB row under the **uploading user** with:
      - a reference to the original asset (video/image) `azureUrl`
      - (videos) a reference to the generated `ThumbnailUrl`
```

### Client access pattern (mobile app & Angular UI)

- After upload, the **API owns the references** for both the asset and (for videos) the generated thumbnail **under the user who uploaded it**.
- Clients (mobile or Angular UI) **only need the `ThumbnailUrl`** to render fast previews—**saving compute and time** by avoiding full video loads or client-side thumbnail generation. Use the original `azureUrl` only when the user opens/plays the asset.

---

## Components

### 🔹 Azure Function (`/functions`)

- **Trigger:** `EventGridTrigger` on Storage `BlobCreated` (and future: `DeleteBlob`).
- **Entry:** `Function1.RunAsync(...)`.
- **Helpers:** `DeserializeBlobUrl(...)`, `IsVideoFile(...)`, `CleanPathSegment(...)`.
- **FFmpeg path (App Service):** `${HOME}/site/wwwroot/ffmpeg/ffmpeg.exe`.
- **Local test trigger URL:**
  ```
  http://localhost:7071/runtime/webhooks/EventGrid?functionName=Function1
  ```

> **Note:** The function **generates & uploads** the thumbnail first (for videos). The HTTP POST to the API is a **notification/metadata update** so the UI/mobile can rely on DB records (instead of listing blobs) and directly use `ThumbnailUrl`.

---

### 🔹 Web API (`/api`)

**Controllers**

- `MediaItemsController`
  - `POST /api/MediaItems/create-media-item` *(JWT)* — create DB entry for file/folder.
  - `POST /api/MediaItems/add-media-item-thumbnail` *(API key)* — link thumbnail to video by `azureUrl`.
  - `GET /api/MediaItems/get-media-items?id=<folderId|null>` *(JWT)* — returns `{ folders, files, path }` for current user.
  - `GET /api/MediaItems/get-media-item-by-id?Id=<id>` *(JWT)*
  - `PATCH /api/MediaItems/patch-media-item` *(JWT)* — rename/update description; validates duplicate names.
  - `DELETE /api/MediaItems/delete-media-item?id=<id>` *(JWT)* — deletes blob and its thumbnail, then DB row.

- `UploadEncodeAndStreamFilesController`
  - `POST /api/UploadEncodeAndStreamFiles/upload-file` *(JWT)* — accepts `multipart/form-data`; streams to `input-{userId}/<filename>`; returns Azure URL.

**Storage layout**

```
input-{userId}/<filename>
input-{userId}/thumbnails/<name>_thumbnail.png
```

---

### 🔹 UI (`/ui`)

- Angular + Tailwind.

## UI uploads, folder placement & thumbnails

**How uploads work (from the Angular UI)**  
1. User selects a file in the UI.  
2. UI sends the file as `multipart/form-data` to **`POST /api/UploadEncodeAndStreamFiles/upload-file`** (JWT required).  
   - The API streams the file to Azure Blob Storage at `input-{userId}/<filename>` and returns the `azureUrl`.  
3. UI then creates/updates the DB record via **`POST /api/MediaItems/create-media-item`**, passing metadata like `name`, `type`, `mediaType`, `folderId` (if any), and the returned `azureUrl`.

**Putting files inside folders**  
- Folders are **DB-only** (virtual). Creating a folder inserts a row with `type="folder"`.  
- When the UI uploads a file “into” a folder, it simply sets **`folderId`** on the media item to the folder’s DB `Id`.  
- No directories are created in Blob Storage; the hierarchy is maintained entirely in the database.

**Thumbnails in the UI (videos)**  
- After a video is uploaded, Azure **Event Grid** triggers the Function: it downloads the blob, runs **FFmpeg**, uploads a thumbnail to  
  `input-{userId}/thumbnails/<name>_thumbnail.png`, and calls  
  **`POST /api/MediaItems/add-media-item-thumbnail`** to store `ThumbnailUrl` in the same DB row as the video.  
- The UI uses **`GET /api/MediaItems/get-media-items?id=<folderId|null>`** to list items; for videos it reads **`ThumbnailUrl`** from the DB and renders that image (fast preview).  
  The original `azureUrl` is used only when the user opens/plays the asset—**saving client compute and time**.


## Folder structure & hierarchy (DB-driven)

- **Folders are virtual (DB only):** When a folder is created, **no container or blob “folder” is created**. A **DB row** with `type="folder"` is inserted instead.
- **Parent/child relationship:** Files and folders store `folderId` pointing to their parent folder’s DB `Id`. This builds a **hierarchical tree** entirely in the database.
- **Browsing & breadcrumbs:** `GET /api/MediaItems/get-media-items?id=<folderId|null>` returns the child items plus a computed `path` by traversing `folderId` parents—used by the UI for breadcrumbs.
- **Benefits:**
  - Keeps Azure Storage **flat and simple**; avoids empty directory artifacts.
  - **Fast renames/moves**: update DB metadata without copying blobs.
  - Enforces **per-directory uniqueness** via API validation (no duplicate names).
- **Deletion:** Deleting a file removes its blob (and video thumbnail) from storage and the DB row. Deleting a folder removes its **hierarchy in DB** (and associated blobs for contained files, per API/trigger logic).

---

## Prerequisites

- **.NET 8/9 SDK** (API & Functions)
- **Node.js 18+** (Angular CLI recommended)
- **Azure Functions Core Tools**
- **Azure Storage** (or **Azurite** locally)
- **FFmpeg** (locally on PATH, or adjust function path)

---

## Run locally

### API

```bash
cd api
dotnet restore
dotnet run
# Serves at https://localhost:7270 (per launchSettings.json)
```

### Functions

```bash
cd functions
dotnet restore
func start
# http://localhost:7071
```

### UI

```bash
cd ui
npm install
npm start   # or: ng serve
# http://localhost:4200
```

---

- **Function runs but thumbnail not uploaded**
  - Ensure `AzureWebJobsStorage` is valid (or Azurite running).
  - Verify container exists (`input-{userId}`) and userId matches.
  - Confirm FFmpeg path and that the video is a supported extension.

- **Function logs show FFmpeg output but file missing**
  - Use `File.Exists(tempThumbnailFilePath)` (not string length) to validate output.
  - Ensure temp directories exist and you don’t delete the **directory** with `File.Delete(...)`. Use:
    ```csharp
    if (Directory.Exists(tempVideoDirectory)) Directory.Delete(tempVideoDirectory, true);
    if (File.Exists(tempThumbnailFilePath)) File.Delete(tempThumbnailFilePath);
    ```

- **Why it “works” without the POST?**  
  Because the thumbnail exists in storage already. The POST only updates your DB so the UI/mobile can query once and use `ThumbnailUrl` for previews.

---

## License
MIT
