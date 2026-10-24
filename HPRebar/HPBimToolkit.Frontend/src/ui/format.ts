const numberFormat = new Intl.NumberFormat('vi-VN');

export const formatNumber = (value: number): string => numberFormat.format(value);

export const formatScale = (scale: number): string => (scale > 0 ? `1:${formatNumber(scale)}` : 'không tỷ lệ');
