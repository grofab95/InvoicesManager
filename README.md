# Invoices Manager

A C# application for automated email processing and attachment management. It fetches new emails from Gmail, processes attachments (images and PDFs), and uploads them to Dropbox.

## Features

- Polls Gmail for new messages every 5 minutes
- Processes image and PDF attachments
- Converts images to PDF before upload
- Uploads processed files to Dropbox
- Marks emails as processed or unknown

## Technologies

- .NET
- Gmail API
- Dropbox API
- PdfSharpCore
- ImageSharp
- Serilog

## Configuration

- Gmail and Dropbox credentials are stored in configuration files.
- OAuth tokens are saved locally (`C:\fgCode\IM\token.json` for Gmail, `C:\fgCode\IM\Dropbox\token.json` for Dropbox).

## Running

1. Configure Gmail and Dropbox credentials.
2. Build and run the application.
3. The service will start polling Gmail and uploading attachments to Dropbox.

## Logging

- Uses Serilog for structured logging.
- Logs errors and processing information.

## License

MIT
