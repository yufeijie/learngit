function plotScatter3(titleName, xLabelName, yLabelName, zLabelName, len, x, y, z, xBest, yBest, zBest)
% plotScatter3 ����άɢ��ͼ
% titleName ������
% xLabelName yLabelName zLabelName ��������
% len ������
% x y z ɢ������
% xBest yBest zBest ��������
figure(6)
if len > 0
scatter3(x(1:len), y(1:len), z(1:len), 10);
hold on
end
scatter3(xBest, yBest, zBest, 20, 'r', 'filled');
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)
end

