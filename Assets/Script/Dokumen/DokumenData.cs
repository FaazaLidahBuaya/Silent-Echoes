using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HalamanDokumen
{
    [Tooltip("Foto atau gambar pindaian halaman dokumen")]
    public Sprite fotoHalaman;

    [Tooltip("Penjelasan atau transkrip tulisan agar mudah dibaca oleh pemain")]
    [TextArea(5, 12)]
    public string teksPenjelasan;
}

[System.Serializable]
public class DokumenItem
{
    [Tooltip("ID unik dokumen (misal: 'dokumen_diary_1')")]
    public string idDokumen = "dokumen_01";

    [Tooltip("Judul dokumen yang tampil di daftar")]
    public string judulDokumen = "Catatan Usang";

    [Tooltip("Ikon untuk tombol di daftar inventory dokumen")]
    public Sprite iconUI;

    [Tooltip("Model 3D dokumen untuk ditampilkan di panel kiri (bisa dirotasi dan zoom)")]
    public GameObject prefabModel3D;

    [Tooltip("Daftar halaman dokumen (foto + teks penjelasan yang sinkron)")]
    public List<HalamanDokumen> daftarHalaman = new List<HalamanDokumen>();
}
