using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.CryptoGift.Dtos;

public class PreGrabCryptoGiftCmd
{
    [Required]
    public Guid Id { get; set; }
}