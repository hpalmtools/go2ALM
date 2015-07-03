# go2ALM: a tool to migrate to QC/ALM

## Purpose
go2ALM was created to migrate from existing solutions to QC/ALM. 
The goal is to make the best possible migration, with the best possible fidelity, 
so that you can move all your data to QC/ALM, for good.

## Features
* Source tools
 * Borland/Microfocus CaliberRM™
* Destination tool
 * QC/ALM (QC 9.2 and above – ALM 11 brings additional capabilities for "requirement" migrations)
 
### CaliberRM
* Select source CaliberRM™ project and destination ALM project & folder
* Select CaliberRM™ baseline to import
* Migrate CaliberRM™ requirements to ALM project
* Preserve CaliberRM™ requirement types in ALM
* CaliberRM™ discussions are migrated to ALM’s "Comments"field
* CaliberRM™ details field (rich text) is migrated to ALM’s "Details" fields and
the complete rich text, included images, is migrated to the new ALM 11 "Rich Text" tab
* CaliberRM™ attachments are migrated as ALM 11 attachments
* Traceability (link) between CaliberRM™ requirements within the same CaliberRM™ projects are preserved in ALM

## Download

Check the [Release](../../releases) section

## Screenshots

![Screenshot](/img/screenshot1.jpg)

![Screenshot](/img/screenshot2.jpg)

![Screenshot](/img/screenshot3.jpg)

![Screenshot](/img/screenshot4.jpg)
