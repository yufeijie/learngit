function plotSurf2(titleName, xLabelName, yLabelName, zLabelName, x, y, z)
% plotSurf2 画三维曲面图
% titleName 标题名
% xLabelName X轴名
% yLabelName Y轴名
% zLabelName Z轴名
% x y z 数据
figure(4)
surf(x, y, z);
colormap(jet);
colorbar;
shading interp;
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)
end