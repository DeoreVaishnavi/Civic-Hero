export const isSupportedImage=file=>["image/jpeg","image/png","image/webp"].includes(file?.type); export const fileSizeMb=file=>(file?.size||0)/1024/1024;
