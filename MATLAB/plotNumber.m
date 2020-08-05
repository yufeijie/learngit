function plotNumber(headline, subTitle1, subTitle2, subTitle3, subTitle4, xLabelName, y1LabelName, y2LabelName, y3LabelName, y4LabelName, color, len1, x1, y1, len2, x2, y2, y3, y4)
% plotNumber 画模块数变化图
% headline 大标题
% xLabelName y1LabelName 2 3 4 坐标轴名
% color 颜色
% len1 len2 数据数
% x1 x2 y1 y2 y3 y4 散点数据
figure(8)

subplot(4,1,4)
scatter(x1(1:len1), y1(1:len1), 10, color, 'filled');
grid on
%title(subTitle1)
xlabel(xLabelName)
ylabel(y1LabelName)

subplot(4,1,3)
scatter(x2(1:len2), y2(1:len2), 10, color, 'filled');
grid on
%title(subTitle2)
%xlabel(xLabelName)
ylabel(y2LabelName)

subplot(4,1,2)
scatter(x2(1:len2), y3(1:len2), 10, color, 'filled');
grid on
%title(subTitle3)
%xlabel(xLabelName)
ylabel(y3LabelName)

subplot(4,1,1)
scatter(x2(1:len2), y4(1:len2), 10, color, 'filled');
grid on
%title(subTitle4)
%xlabel(xLabelName)
ylabel(y4LabelName)

%suptitle(headline)
end

